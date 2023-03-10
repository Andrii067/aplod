using System.Threading;
using AxMSTSCLib;

namespace VdsFileUploader
{
	internal class ThrParams
	{
		private ManualResetEvent _ts;

		private AxMsRdpClient6NotSafeForScripting _rdp;

		public ManualResetEvent Ts
		{
			get
			{
				return _ts;
			}
			set
			{
				_ts = value;
			}
		}

		public AxMsRdpClient6NotSafeForScripting Rdp
		{
			get
			{
				return _rdp;
			}
			set
			{
				_rdp = value;
			}
		}
	}
}
