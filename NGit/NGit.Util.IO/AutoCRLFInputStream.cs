/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit.Diff;
using Sharpen;

namespace NGit.Util.IO
{
	/// <summary>An OutputStream that expands LF to CRLF.</summary>
	/// <remarks>
	/// An OutputStream that expands LF to CRLF.
	/// Existing CRLF are not expanded to CRCRLF, but retained as is.
	/// Optionally, a binary check on the first 8000 bytes is performed and in case
	/// of binary files, canonicalization is turned off (for the complete file).
	/// </remarks>
	public class AutoCRLFInputStream : InputStream
	{
		private readonly byte[] single = new byte[1];

		private readonly byte[] buf = new byte[8096];

		private readonly InputStream @in;

		private int cnt;

		private int ptr;

		private bool isBinary;

		private bool detectBinary;

		private byte last;

		/// <summary>Creates a new InputStream, wrapping the specified stream</summary>
		/// <param name="in">raw input stream</param>
		/// <param name="detectBinary">whether binaries should be detected</param>
		/// <since>2.0</since>
		public AutoCRLFInputStream(InputStream @in, bool detectBinary)
		{
			this.@in = @in;
			this.detectBinary = detectBinary;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			int read = Read(single, 0, 1);
			return read == 1 ? single[0] & unchecked((int)(0xff)) : -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] bs, int off, int len)
		{
			if (len == 0)
			{
				return 0;
			}
			if (cnt == -1)
			{
				return -1;
			}
			int startOff = off;
			int end = off + len;
			while (off < end)
			{
				if (ptr == cnt && !FillBuffer())
				{
					break;
				}
				byte b = buf[ptr++];
				if (isBinary || b != '\n')
				{
					// Logic for binary files ends here
					bs[off++] = last = b;
					continue;
				}
				if (b == '\n')
				{
					if (last == '\r')
					{
						bs[off++] = last = b;
						continue;
					}
					bs[off++] = last = (byte)('\r');
					ptr--;
				}
				else
				{
					bs[off++] = last = b;
				}
			}
			int n = startOff == off ? -1 : off - startOff;
			if (n > 0)
			{
				last = bs[off - 1];
			}
			return n;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool FillBuffer()
		{
			cnt = @in.Read(buf, 0, buf.Length);
			if (cnt < 1)
			{
				return false;
			}
			if (detectBinary)
			{
				isBinary = RawText.IsBinary(buf, cnt);
				detectBinary = false;
			}
			ptr = 0;
			return true;
		}
	}
}
