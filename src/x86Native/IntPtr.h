#pragma once

namespace x86Emulator
{
	namespace UWP
	{
		namespace Native
		{
#ifdef _WIN64
			typedef int64 IntPtr;
#else
			typedef int32 IntPtr;
#endif
		}
	}
}