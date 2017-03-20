using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

	class UrwObject {
		public UrwObjectType type;
		public string name;
		public string group;
		public float weight;
		public float weight2;
		public float value;
		public byte spriteIndex;
		public byte carbohydrates;
		public byte fat;
		public byte proteins;

		public byte damageBlunt;
		public byte damageEdge;
		public byte damagePoint;

		public byte tear;
		public byte squeeze;
		public byte warmth;

		public byte mature;
		public byte sprout;
		public byte wither;

		public byte water;
		public float valuePerLbs;
		public byte quality;

		public byte h1Penalty;

		public byte attackBonus;
		public byte defenceBonus;

		public void Deserialize(BinaryReader data) {
			byte[] bytes;

			this.type = (UrwObjectType)data.ReadByte();

			data.ReadBytes(8); // Unknown

			bytes = data.ReadBytes(40);
			this.name = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));
			bytes = data.ReadBytes(40);
			this.group = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));

			data.ReadBytes(1); // Unknown

			this.spriteIndex = data.ReadByte();

			data.ReadBytes(1); // Unknown

			this.value = data.ReadSingle();

			if (type == UrwObjectType.Weapon || type == UrwObjectType.Armor) {
				this.damageBlunt = data.ReadByte();
				this.damageEdge = data.ReadByte();
				this.damagePoint = data.ReadByte();

				this.tear = data.ReadByte();
				this.squeeze = data.ReadByte();
				this.warmth = data.ReadByte();
			} else if (type == UrwObjectType.Food || type == UrwObjectType.Plant) {
				this.carbohydrates = data.ReadByte();
				this.fat = data.ReadByte();
				this.proteins = data.ReadByte();

				this.mature = data.ReadByte();
				this.sprout = data.ReadByte();
				this.wither = data.ReadByte();
			} else {
				data.ReadBytes(6);
			}

			data.ReadBytes(4); // Unknown

			this.h1Penalty = data.ReadByte();

			data.ReadBytes(1); // Unknown

			this.accuracy = data.ReadByte();

			data.ReadBytes(3); // Unknown

			this.weight = data.ReadSingle();  // weight for containers and wear?
			this.weight2 = data.ReadSingle(); // weight or capacity

			data.ReadBytes(28); // Unknown

			this.water = data.ReadByte();

			data.ReadBytes(3); // Unknown

			this.valuePerLbs = data.ReadSingle();

			data.ReadBytes(9); // Unknown

			this.quality = (byte)(data.ReadByte() & 0x0F);

			data.ReadBytes(1); // Unknown

			if (type == UrwObjectType.Weapon) {
				int combatBonuses = data.ReadByte();
				this.attackBonus = (byte)(combatBonuses & 0x0F);
				this.defenceBonus = (byte)(combatBonuses >> 4);
			}

			data.ReadBytes(4); // Unknown
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
			new FieldWriter("Name", (o) => QuoteString(o.name.ToString())),
			new FieldWriter("Group", (o) => QuoteString(o.group.ToString())),
			new FieldWriter("Value", (o) => o.value.ToString()),
			new FieldWriter("Weight", (o) => o.weight.ToString()),
			new FieldWriter("Weight2", (o) => o.weight2.ToString()),
			new FieldWriter("Sprite", (o) => o.spriteIndex.ToString()),
			new FieldWriter("Carbohydrates", (o) => o.carbohydrates.ToString()),
			new FieldWriter("Fat", (o) => o.fat.ToString()),
			new FieldWriter("Proteins", (o) => o.proteins.ToString()),

			new FieldWriter("Blunt", (o) => FormatDamage(o.damageBlunt)),
			new FieldWriter("Edge", (o) => FormatDamage(o.damageEdge)),
			new FieldWriter("Point", (o) => FormatDamage(o.damagePoint)),
			new FieldWriter("Tear", (o) => FormatDamage(o.tear)),
			new FieldWriter("Squeeze", (o) => FormatDamage(o.squeeze)),
			new FieldWriter("Warmth", (o) => FormatDamage(o.warmth)),

			new FieldWriter("1HPenalty", (o) => o.h1Penalty.ToString()),
			new FieldWriter("Accuracy", (o) => o.accuracy.ToString()),
			new FieldWriter("AttackBonus", (o) => o.attackBonus.ToString()),
			new FieldWriter("DefenceBonus", (o) => o.defenceBonus.ToString()),

			new FieldWriter("Mature", (o) => o.mature.ToString()),
			new FieldWriter("Sprout", (o) => o.sprout.ToString()),
			new FieldWriter("Wither", (o) => o.wither.ToString()),

			new FieldWriter("Water", (o) => o.water.ToString()),
			new FieldWriter("Value Per Lbs", (o) => o.valuePerLbs.ToString()),
			new FieldWriter("Quality", (o) => o.quality.ToString()),
		};

		static string QuoteString(string value) {
			return "\"" + value.Replace("\"", "\"\"") + '\"';
		}

		static string FormatDamage(byte value) {
			return value == 200 ? "-" : value.ToString();
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
