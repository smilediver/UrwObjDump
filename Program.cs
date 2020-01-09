using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace UrwObjDump {
    enum UrwObjectType: byte {
        Unused = 0,
        Weapon = 1,
        Armor = 2,
        Container = 3,
        Map = 4,
        // 5 not used
        Tool = 6,
        Food = 7,
        Wood = 8,
        Watercraft = 9,
        Jewelry = 10,
        Plant = 11,
    }

    struct Nutrition {
        public byte carbohydrates;
        public byte fat;
        public byte proteins;
    }

    struct CombatInfo {
        public byte damageBlunt;
        public byte damageEdge;
        public byte damagePoint;

        public byte tear;
        public byte squeeze;
        public byte warmth;
    }

    struct PlantInfo {
        public byte mature;
        public byte sprout;
        public byte wither;
    }

    class UrwObject {
        public UrwObjectType type;
        public string name;
        public string group;
        public string sprite;
        public float weight;
        public float weight2;
        public float value;
        public byte spriteIndex;
        public Nutrition nutrition;
        public CombatInfo combatInfo;
        public PlantInfo plantInfo;

        public byte water;
        public float valuePerLbs;
        public byte quality;

        public byte h1Penalty;
        public byte accuracy;
        public byte attackBonus;
        public byte defenseBonus;

        public void Deserialize(BinaryReader data) {
            byte[] bytes;

            this.type = (UrwObjectType)data.ReadByte();

            data.ReadBytes(8); // Unknown

            bytes = data.ReadBytes(40);
            this.name = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));
            bytes = data.ReadBytes(27);
            this.group = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));
            bytes = data.ReadBytes(13);
            this.sprite = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));

            data.ReadBytes(1); // Unknown

            this.spriteIndex = data.ReadByte();

            data.ReadBytes(1); // Unknown

            this.value = data.ReadSingle();

            if (type == UrwObjectType.Weapon || type == UrwObjectType.Armor) {
                this.combatInfo = ReadCombatInfo(data);
            } else if (type == UrwObjectType.Food || type == UrwObjectType.Plant) {
                this.nutrition = ReadNutritionInfo(data);
                this.plantInfo = ReadPlantInfo(data);
            } else {
                data.ReadBytes(6);
            }

            data.ReadBytes(4); // Unknown

            if (type == UrwObjectType.Weapon) {
                this.h1Penalty = data.ReadByte();
                data.ReadBytes(1); // Unknown
                this.accuracy = data.ReadByte();
            } else {
                data.ReadBytes(3); // Unknown
            }

            data.ReadBytes(3); // Unknown

            this.weight = data.ReadSingle();  // weight for containers and wear?
            this.weight2 = data.ReadSingle(); // weight or capacity

            data.ReadBytes(22); // Unknown

            this.water = data.ReadByte();

            data.ReadBytes(3); // Unknown

            this.valuePerLbs = data.ReadSingle();

            data.ReadBytes(15); // Unknown

            this.quality = (byte)(data.ReadByte() & 0x0F);

            data.ReadBytes(1); // Unknown

            if (type == UrwObjectType.Weapon) {
                int combatBonuses = data.ReadByte();
                this.attackBonus = (byte)(combatBonuses & 0x0F);
                this.defenseBonus = (byte)(combatBonuses >> 4);
            }
            else {
                data.ReadBytes(1);
            }

            data.ReadBytes(4); // Unknown
        }

        private PlantInfo ReadPlantInfo(BinaryReader data) {
            PlantInfo info;
            info.mature = data.ReadByte();
            info.sprout = data.ReadByte();
            info.wither = data.ReadByte();
            return info;
        }

        private Nutrition ReadNutritionInfo(BinaryReader data) {
            Nutrition info;
            info.carbohydrates = data.ReadByte();
            info.fat = data.ReadByte();
            info.proteins = data.ReadByte();
            return info;
        }

        CombatInfo ReadCombatInfo(BinaryReader data) {
            CombatInfo info;

            info.damageBlunt = data.ReadByte();
            info.damageEdge = data.ReadByte();
            info.damagePoint = data.ReadByte();

            info.tear = data.ReadByte();
            info.squeeze = data.ReadByte();
            info.warmth = data.ReadByte();

            return info;
        }

        public UrwObject(BinaryReader data) {
            Deserialize(data);
        }
    }

    class Formatting {
        public struct FieldWriter {
            public readonly string name;
            public readonly Func<UrwObject, string> formatter;

            public FieldWriter(string name, Func<UrwObject, string> formatter) {
                this.name = name;
                this.formatter = formatter;
            }
        }

        public static FieldWriter[] fieldWriters = new[]{
            new FieldWriter("Type", (o) => o.type.ToString()),
            new FieldWriter("Name", (o) => QuoteString(o.name)),
            new FieldWriter("Group", (o) => QuoteString(o.group)),
            new FieldWriter("Sprite", (o) => QuoteString(o.sprite)),
            new FieldWriter("Sprite", (o) => o.spriteIndex.ToString()),
            new FieldWriter("Value", (o) => o.value.ToString(CultureInfo.InvariantCulture)),
            new FieldWriter("Weight", (o) => o.weight.ToString(CultureInfo.InvariantCulture)),
            new FieldWriter("Weight2", (o) => o.weight2.ToString(CultureInfo.InvariantCulture)),
            new FieldWriter("Carbohydrates", (o) => o.nutrition.carbohydrates.ToString()),
            new FieldWriter("Fat", (o) => o.nutrition.fat.ToString()),
            new FieldWriter("Proteins", (o) => o.nutrition.proteins.ToString()),

            new FieldWriter("Blunt", (o) => FormatDamage(o.combatInfo.damageBlunt)),
            new FieldWriter("Edge", (o) => FormatDamage(o.combatInfo.damageEdge)),
            new FieldWriter("Point", (o) => FormatDamage(o.combatInfo.damagePoint)),
            new FieldWriter("Tear", (o) => FormatDamage(o.combatInfo.tear)),
            new FieldWriter("Squeeze", (o) => FormatDamage(o.combatInfo.squeeze)),
            new FieldWriter("Warmth", (o) => FormatDamage(o.combatInfo.warmth)),

            new FieldWriter("1HPenalty", (o) => o.h1Penalty.ToString()),
            new FieldWriter("Accuracy", (o) => o.accuracy.ToString()),
            new FieldWriter("AttackBonus", (o) => o.attackBonus.ToString()),
            new FieldWriter("DefenseBonus", (o) => o.defenseBonus.ToString()),

            new FieldWriter("Mature", (o) => o.plantInfo.mature.ToString()),
            new FieldWriter("Sprout", (o) => o.plantInfo.sprout.ToString()),
            new FieldWriter("Wither", (o) => o.plantInfo.wither.ToString()),

            new FieldWriter("Water", (o) => o.water.ToString()),
            new FieldWriter("Value Per Lbs", (o) => o.valuePerLbs.ToString(CultureInfo.InvariantCulture)),
            new FieldWriter("Quality", (o) => o.quality.ToString()),
        };

        static string QuoteString(string value) {
            return "\"" + value.Replace("\"", "\"\"") + '\"';
        }

        static string FormatDamage(byte value) {
            return value == 200 ? "0" : value.ToString();
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: UrwDump <obj file> <dump file>");
                Environment.Exit(1);
            }

            var data = new BinaryReader(File.OpenRead(args[0]));
            TextWriter output = File.CreateText(args[1]);
            output.WriteLine(string.Join(",", Formatting.fieldWriters.Select((f) => f.name)));
            while (data.PeekChar() >= 0)
            {
                var o = new UrwObject(data);
                output.WriteLine(string.Join(",", Formatting.fieldWriters.Select((f) => f.formatter(o))));
            }

            output.Flush();
            //Console.ReadKey();
        }
    }
}
