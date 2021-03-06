//
// System.Security.Policy.Evidence
//
// Authors:
//	Sean MacIsaac (macisaac@ximian.com)
//	Nick Drochak (ndrochak@gol.com)
//	Jackson Harper (Jackson@LatitudeGeo.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2001 Ximian, Inc.
// Portions (C) 2003, 2004 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Cryptography.X509Certificates;

#if !NET_2_1 || MONOTOUCH
using Mono.Security.Authenticode;
#endif

namespace System.Security.Policy {

	[Serializable]
	[MonoTODO ("Serialization format not compatible with .NET")]
#if NET_2_0
	[ComVisible (true)]
#endif
	public sealed class Evidence : ICollection, IEnumerable {
	
		private bool _locked;
		private ArrayList hostEvidenceList;	
		private ArrayList assemblyEvidenceList;
#if NET_2_0		
		private int _hashCode;
#endif

		public Evidence () 
		{
		}

		public Evidence (Evidence evidence)
		{
			if (evidence != null)
				Merge (evidence);	
		}

		public Evidence (object[] hostEvidence, object[] assemblyEvidence)
		{
			if (null != hostEvidence)
				HostEvidenceList.AddRange (hostEvidence);
			if (null != assemblyEvidence)
				AssemblyEvidenceList.AddRange (assemblyEvidence);
		}
		
		//
		// Public Properties
		//
	
		public int Count {
			get {
				int count = 0;
				if (hostEvidenceList != null)
					count += hostEvidenceList.Count;
				if (assemblyEvidenceList!= null)
					count += assemblyEvidenceList.Count;
				return count;
			}
		}

		public bool IsReadOnly {
			get{ return false; }
		}
		
		public bool IsSynchronized {
#if NET_2_0
			get { return false; }
#else
			// LAMESPEC: Always TRUE (not FALSE)
			get { return true; }
#endif
		}

		public bool Locked {
			get { return _locked; }
			[SecurityPermission (SecurityAction.Demand, ControlEvidence = true)]
			set { 
				_locked = value; 
			}
		}	

		public object SyncRoot {
			get { return this; }
		}

		internal ArrayList HostEvidenceList {
			get {
				if (hostEvidenceList == null)
					hostEvidenceList = ArrayList.Synchronized (new ArrayList ());
				return hostEvidenceList;
			}
		}

		internal ArrayList AssemblyEvidenceList {
			get {
				if (assemblyEvidenceList == null)
					assemblyEvidenceList = ArrayList.Synchronized (new ArrayList ());
				return assemblyEvidenceList;
			}
		}

		//
		// Public Methods
		//

		public void AddAssembly (object id) 
		{
			AssemblyEvidenceList.Add (id);
#if NET_2_0
			_hashCode = 0;
#endif			
		}

		public void AddHost (object id) 
		{
			if (_locked && SecurityManager.SecurityEnabled) {
				new SecurityPermission (SecurityPermissionFlag.ControlEvidence).Demand ();
			}
			HostEvidenceList.Add (id);
#if NET_2_0
			_hashCode = 0;
#endif			
		}

#if NET_2_0
		[ComVisible (false)]
		public void Clear ()
		{
			if (hostEvidenceList != null)
				hostEvidenceList.Clear ();
			if (assemblyEvidenceList != null)
				assemblyEvidenceList.Clear ();
			_hashCode = 0;
		}
#endif

		public void CopyTo (Array array, int index) 
		{
			int hc = 0;
			if (hostEvidenceList != null) {
				hc = hostEvidenceList.Count;
				if (hc > 0)
					hostEvidenceList.CopyTo (array, index);
			}
			if ((assemblyEvidenceList != null) && (assemblyEvidenceList.Count > 0))
				assemblyEvidenceList.CopyTo (array, index + hc);
		}

#if NET_2_0
		[ComVisible (false)]
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			Evidence e = (obj as Evidence);
			if (e == null)
				return false;

			if (HostEvidenceList.Count != e.HostEvidenceList.Count)
				return false;
			if (AssemblyEvidenceList.Count != e.AssemblyEvidenceList.Count)
				return false;

			for (int i = 0; i < hostEvidenceList.Count; i++) {
				bool found = false;
				for (int j = 0; j < e.hostEvidenceList.Count; i++) {
					if (hostEvidenceList [i].Equals (e.hostEvidenceList [j])) {
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			for (int i = 0; i < assemblyEvidenceList.Count; i++) {
				bool found = false;
				for (int j = 0; j < e.assemblyEvidenceList.Count; i++) {
					if (assemblyEvidenceList [i].Equals (e.assemblyEvidenceList [j])) {
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			
			return true;
		}
#endif

		public IEnumerator GetEnumerator () 
		{
			IEnumerator he = null;
			if (hostEvidenceList != null)
				he = hostEvidenceList.GetEnumerator ();
			IEnumerator ae = null;
			if (assemblyEvidenceList != null)
				ae = assemblyEvidenceList.GetEnumerator ();
			return new EvidenceEnumerator (he, ae);
		}

		public IEnumerator GetAssemblyEnumerator () 
		{
			return AssemblyEvidenceList.GetEnumerator ();
		}

#if NET_2_0
		[ComVisible (false)]
		public override int GetHashCode ()
		{
			// kind of long so we cache it
			if (_hashCode == 0) {
				if (hostEvidenceList != null) {
					for (int i = 0; i < hostEvidenceList.Count; i++)
						_hashCode ^= hostEvidenceList [i].GetHashCode ();
				}
				if (assemblyEvidenceList != null) {
					for (int i = 0; i < assemblyEvidenceList.Count; i++)
						_hashCode ^= assemblyEvidenceList [i].GetHashCode ();
				}
			}
			return _hashCode;
		}
#endif

		public IEnumerator GetHostEnumerator () 
		{
			return HostEvidenceList.GetEnumerator ();
		}

		public void Merge (Evidence evidence) 
		{
			if ((evidence != null) && (evidence.Count > 0)) {
				if (evidence.hostEvidenceList != null) {
					foreach (object o in evidence.hostEvidenceList)
						AddHost (o);
				}
				if (evidence.assemblyEvidenceList != null) {
					foreach (object o in evidence.assemblyEvidenceList)
						AddAssembly (o);
				}
#if NET_2_0
				_hashCode = 0;
#endif			
			}
		}

#if NET_2_0
		[ComVisible (false)]
		public void RemoveType (Type t)
		{
			for (int i = hostEvidenceList.Count; i >= 0; i--) {
				if (hostEvidenceList.GetType () == t) {
					hostEvidenceList.RemoveAt (i);
					_hashCode = 0;
				}
			}
			for (int i = assemblyEvidenceList.Count; i >= 0; i--) {
				if (assemblyEvidenceList.GetType () == t) {
					assemblyEvidenceList.RemoveAt (i);
					_hashCode = 0;
				}
			}
		}
#endif

		// Use an icall to avoid multiple file i/o to detect the 
		// "possible" presence of an Authenticode signature
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool IsAuthenticodePresent (Assembly a);
#if NET_2_1
		static internal Evidence GetDefaultHostEvidence (Assembly a)
		{
			return new Evidence ();
		}
#else
		// this avoid us to build all evidences from the runtime
		// (i.e. multiple unmanaged->managed calls) and also allows
		// to delay their creation until (if) needed
		[FileIOPermission (SecurityAction.Assert, Unrestricted = true)]
		static internal Evidence GetDefaultHostEvidence (Assembly a) 
		{
			Evidence e = new Evidence ();
			string aname = a.EscapedCodeBase;

			// by default all assembly have the Zone, Url and Hash evidences
			e.AddHost (Zone.CreateFromUrl (aname));
			e.AddHost (new Url (aname));
			e.AddHost (new Hash (a));

			// non local files (e.g. http://) also get a Site evidence
			if (String.Compare ("FILE://", 0, aname, 0, 7, true, CultureInfo.InvariantCulture) != 0) {
				e.AddHost (Site.CreateFromUrl (aname));
			}

			// strongnamed assemblies gets a StrongName evidence
			AssemblyName an = a.UnprotectedGetName ();
			byte[] pk = an.GetPublicKey ();
			if ((pk != null) && (pk.Length > 0)) {
				StrongNamePublicKeyBlob blob = new StrongNamePublicKeyBlob (pk);
				e.AddHost (new StrongName (blob, an.Name, an.Version));
			}

			// Authenticode(r) signed assemblies get a Publisher evidence
			if (IsAuthenticodePresent (a)) {
				// Note: The certificate is part of the evidences even if it is not trusted!
				// so we can't call X509Certificate.CreateFromSignedFile
				AuthenticodeDeformatter ad = new AuthenticodeDeformatter (a.Location);
				if (ad.SigningCertificate != null) {
					X509Certificate x509 = new X509Certificate (ad.SigningCertificate.RawData);
					if (x509.GetHashCode () != 0) {
						e.AddHost (new Publisher (x509));
					}
				}
			}
#if NET_2_0
			// assemblies loaded from the GAC also get a Gac evidence (new in Fx 2.0)
			if (a.GlobalAssemblyCache) {
				e.AddHost (new GacInstalled ());
			}

			// the current HostSecurityManager may add/remove some evidence
			AppDomainManager dommgr = AppDomain.CurrentDomain.DomainManager;
			if (dommgr != null) {
				if ((dommgr.HostSecurityManager.Flags & HostSecurityManagerOptions.HostAssemblyEvidence) ==
					HostSecurityManagerOptions.HostAssemblyEvidence) {
					e = dommgr.HostSecurityManager.ProvideAssemblyEvidence (a, e);
				}
			}
#endif
			return e;
		}

#endif // NET_2_1

		private class EvidenceEnumerator : IEnumerator {
			
			private IEnumerator currentEnum, hostEnum, assemblyEnum;		
	
			public EvidenceEnumerator (IEnumerator hostenum, IEnumerator assemblyenum) 
			{
				this.hostEnum = hostenum;
				this.assemblyEnum = assemblyenum;
				currentEnum = hostEnum;		
			}

			public bool MoveNext () 
			{
				if (currentEnum == null)
					return false;

				bool ret = currentEnum.MoveNext ();
				
				if (!ret && (hostEnum == currentEnum) && (assemblyEnum != null)) {
					currentEnum = assemblyEnum;
					ret = assemblyEnum.MoveNext ();
				}

				return ret;
			}

			public void Reset () 
			{
				if (hostEnum != null) {
					hostEnum.Reset ();
					currentEnum = hostEnum;
				} else {
					currentEnum = assemblyEnum;
				}
				if (assemblyEnum != null)
					assemblyEnum.Reset ();
			}

			public object Current {
				get {
					return currentEnum.Current;
				}
			}
		}
	}
}

