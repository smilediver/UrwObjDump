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

		public void Deserialize(BinaryReader data) {
			byte[] bytes;

			this.type = (UrwObjectType)data.ReadByte();
			data.ReadBytes(8); // Unknown
			bytes = data.ReadBytes(40);
			this.name = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));
			bytes = data.ReadBytes(40);
			this.group = Encoding.UTF8.GetString(bytes, 0, Array.FindIndex(bytes, (b)=>b==0));
			data.ReadBytes(3);
			this.value = data.ReadSingle();
			data.ReadBytes(16); // Unknown

			this.weight = data.ReadSingle();
			this.weight2 = data.ReadSingle();
			data.ReadBytes(52); // Unknown

		}

		public UrwObject(BinaryReader data) {
			Deserialize(data);
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
			output.WriteLine(string.Join(",", new string[]{
				"Type",
				"Name",
				"Group",
				"Value",
				"Weight",
				"Weight2",
			}));
			while (data.PeekChar() >= 0)
			{
				DumpObject(data, output);
			}

			output.Flush();

			//Console.ReadKey();
		}

		private static void DumpObject(BinaryReader data, TextWriter output)
		{
			var o = new UrwObject(data);

			//Console.WriteLine("{0} {1} V:{2,3} W:{3,3} W:{4,3} {5} {6}",
			//	o.name, o.group, o.value, o.weight, o.weight2, 0, 0);
			var l = new List<string>();

			l.Add(o.type.ToString());
			l.Add('\"' + o.name.ToString().Replace("\"", "\"\"") + '\"');
			l.Add('\"' + o.group.ToString().Replace("\"", "\"\"")+ '\"');
			l.Add(o.value.ToString());
			l.Add(o.weight.ToString());
			l.Add(o.weight2.ToString());

			Write(output, l);
		}

		private static void Write(TextWriter output, List<string> values)
		{
			var str = string.Join(",", values);
			output.WriteLine(str);
			//Console.WriteLine(str);
		}
	}
}
