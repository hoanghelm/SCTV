using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streaming.Service.WebRTC
{
	public class WebRTCConfiguration
	{
		public List<IceServerConfig> IceServers { get; set; } = new List<IceServerConfig>
		{
			new IceServerConfig { Url = "stun:stun.l.google.com:19302" }
		};

		public int VideoWidth { get; set; } = 1280;
		public int VideoHeight { get; set; } = 720;
		public int VideoFrameRate { get; set; } = 30;
		public VideoCodecsEnum VideoCodec { get; set; } = VideoCodecsEnum.VP8;
	}

	public class IceServerConfig
	{
		public string Url { get; set; }
		public string Username { get; set; }
		public string Credential { get; set; }
	}
}
