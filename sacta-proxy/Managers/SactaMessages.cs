using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

using sacta_proxy.helpers;
using sacta_proxy.model;

namespace sacta_proxy.Managers
{
	class PsiOrScvInfo
	{
		public int LastSectMsgId = -1;
		public uint LastSectVersion = 0;
	}

	[Serializable]
	public class SactaMsg
	{
		public enum MsgType : ushort { Init = 0, SectAsk = 707, SectAnswer = 710, Presence = 1530, Sectorization = 1632 }
		public const ushort InitId = 0x4000;

		[Serializable]
		[XmlInclude(typeof(PresenceInfo))]
		[XmlInclude(typeof(SectInfo))]
		[XmlInclude(typeof(SectAnswerInfo))]
		public class DataInfoBase { }

		[Serializable]
		public class PresenceInfo : DataInfoBase
		{
			public ushort NumTPChars = 3;

			[SerializeAs(Length = 10, ElementSize = 1)]
			public byte[] ProcessorType = new byte[10] { 0x53, 0x43, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

			public ushort ProcessorNumber = 1;
			public ushort Reserved = 0;
			public byte ProcessorState = 1;
			public byte ProcessorSubState = 0;
			public ushort PresencePerioditySg = 5;
			public ushort ActivityTimeOutSg = 30;
		}

		[Serializable]
		public class SectInfo : DataInfoBase
		{
			[Serializable]
			public class SectorInfo
			{
				public byte PCVSacta = 0;
				public byte Reserved = 0;

				[SerializeAs(Size = 4)]
				public string SectorCode = "";

				public byte Ucs = 0;
				public byte UcsType = 2;

                public override string ToString()
                {
                    return $"{SectorCode}:{Ucs}";
                }
            }

			public uint Version = 0;
			public ushort Reserved = 0;
			public ushort NumSectors = 0;

			[SerializeAs(LengthField = "NumSectors")]
			public SectorInfo[] Sectors;
			public SectInfo(uint version, string[] sectorUcs)
			{
				Version = version;
				NumSectors = (ushort)(sectorUcs.Length / 2);
				Sectors = new SectorInfo[NumSectors];

				for (int i = 0; i < NumSectors; i++)
				{
					Sectors[i] = new SectorInfo();
					Sectors[i].SectorCode = sectorUcs[i * 2];
					Sectors[i].Ucs = Byte.Parse(sectorUcs[(i * 2) + 1]);
				}
			}	
			public SectInfo(uint version, Dictionary<string, int> SectorMap)
            {
				Version = version;
				NumSectors = (ushort)SectorMap.Count();
				Sectors = SectorMap.Select(sm => new SectorInfo() { SectorCode = sm.Key, Ucs = (byte)sm.Value, UcsType = 0 }).ToArray();
            }
		}

		[Serializable]
		public class SectAnswerInfo : DataInfoBase
		{
			public uint Version = 0;
			public byte Result = 0;
			public byte Reserved = 0;
            public SectAnswerInfo(uint version = 0, byte result = 0)
            {
                Version = version;
                Result = result;
            }
		}

		public byte DomainOrg = 0;
		public byte CenterOrg = 0;
		public ushort UserOrg = 0;
		public byte DomainDst = 0;
		public byte CenterDst = 0;
		public ushort UserDst = 0;
		public ushort Session = 0;
		public MsgType Type;
		public ushort Id;
		public ushort Length;
		public uint Hour;
		[SerializeAs(RuntimeFieldType = "GetRuntimeParamType")]
		public DataInfoBase Info;
		public Type GetRuntimeParamType()
		{
			switch (Type)
			{
				case MsgType.Init:
				case MsgType.SectAsk:
					return typeof(DataInfoBase);
				case MsgType.Presence:
					return typeof(PresenceInfo);
				case MsgType.Sectorization:
					return typeof(SectInfo);
				case MsgType.SectAnswer:
					return typeof(SectAnswerInfo);
				default:
					throw new Exception("Invalid SactaMsg type (" + (int)Type + ")");
			}
		}
        public SactaMsg(MsgType type, int id, int seq, Dictionary<string,int> sectorUcs=null, int version = 0, int result = 0)
        {
			Type = type;
            Id = (ushort)((id & 0xE000) | (seq & 0x1FFF));
            Hour = (uint)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            switch (type)
            {
                case MsgType.Presence:
                    Length = 11;
                    Info = new PresenceInfo();
                    break;
                case MsgType.SectAnswer:
                    Length = 3;
                    Info = new SectAnswerInfo((uint)version, (byte)result);
                    break;
				case MsgType.Sectorization:
					Info = new SectInfo((uint)version, sectorUcs);
					Length = (ushort)(4 + (4 * ((SectInfo)Info).NumSectors));
					break;
			}
		}
		public byte[] Serialize()
        {
            CustomBinaryFormatter bf = new CustomBinaryFormatter(); 
            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, this);
            return ms.ToArray();
        }
        public override string ToString()
        {
			var Origen = $"{DomainOrg}-{CenterOrg}-{UserOrg}";
			var Destino = $"{DomainDst}-{CenterDst}-{UserDst}";

            return $"SactaMsg: Origen {Origen}, Destino {Destino}, Tipo {Type}, Id {Id}";
        }
        public static void Deserialize(byte[] data, Action<SactaMsg> deliver, Action<string> deliverError)
        {
            try
            {
				MemoryStream ms = new MemoryStream(data);
				CustomBinaryFormatter bf = new CustomBinaryFormatter();
				SactaMsg msg = bf.Deserialize<SactaMsg>(ms);
				deliver(msg);
			}
			catch(Exception x)
            {
				Logger.Exception<SactaMsg>(x);
				deliverError(x.Message);
            }
        }
		public static SactaMsg MsgToSacta(Configuration.DependecyConfig cfg, MsgType type, int id, int seq, int version = 0, int result = 0)
		{
			var msg = new SactaMsg(type, id, seq, null, version, result)
			{
				DomainOrg = (byte)cfg.SactaProtocol.Scv.Domain,
				CenterOrg = (byte)cfg.SactaProtocol.Scv.Center,
				UserOrg = (ushort)cfg.SactaProtocol.Scv.Scv,
				DomainDst = (byte)cfg.SactaProtocol.Sacta.Domain,
				CenterDst = (byte)cfg.SactaProtocol.Sacta.Center,
				UserDst = (ushort)cfg.SactaProtocol.Sacta.PsiGroup
			};
			if (type == MsgType.Presence)
			{
				(msg.Info as PresenceInfo).PresencePerioditySg = (ushort)cfg.SactaProtocol.TickAlive;
				(msg.Info as PresenceInfo).ActivityTimeOutSg = (ushort)cfg.SactaProtocol.TimeoutAlive;
			}
			return msg;
		}
		public static SactaMsg MsgToScv(Configuration.DependecyConfig cfg, MsgType type, int id, int seq, int version=0, Dictionary<string,int> sectorUcs = null)
		{
			var msg = new SactaMsg(type, id, seq, sectorUcs, version)
			{
				DomainOrg = (byte)cfg.SactaProtocol.Sacta.Domain,
				CenterOrg = (byte)cfg.SactaProtocol.Sacta.Center,
				UserOrg =  type== MsgType.Init || type== MsgType.Presence ? (ushort)cfg.SactaProtocol.Sacta.SpvsList()[0] : (ushort)cfg.SactaProtocol.Sacta.PsisList()[0],
				DomainDst = (byte)cfg.SactaProtocol.Scv.Domain,
				CenterDst = (byte)cfg.SactaProtocol.Scv.Center,
				UserDst = (ushort)cfg.SactaProtocol.Scv.Scv
			};
			if (type == MsgType.Presence)
			{
				(msg.Info as PresenceInfo).PresencePerioditySg = (ushort)cfg.SactaProtocol.TickAlive;
				(msg.Info as PresenceInfo).ActivityTimeOutSg = (ushort)cfg.SactaProtocol.TimeoutAlive;
			}
			return msg;
		}
	}
}
