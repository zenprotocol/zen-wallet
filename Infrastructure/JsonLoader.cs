using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace Infrastructure
{
	public class JsonLoader<T> : Singleton<JsonLoader<T>> where T : class, new()
	{
        private object _sync = new object();
		public event Action OnSaved;

		private String _FileName = null;
		public String FileName { 
			set {
				_FileName = value;
			}
		}

		private bool _IsNew;
		public bool IsNew { 
			get {
				Ensure ();
				return _IsNew;
			}
		}

		private bool _Corrupt;
		public bool Corrupt { 
			get {
				Ensure ();
				return _Corrupt;
			}
		}

		private T _Value = null;
		public T Value {
			get {
				Ensure ();
				return _Value; 
			} 
			set
            {
                _Value = value;
            }
		}

		private void Ensure ()
		{
            lock (_sync)
            {
                if (_Value != null)
                {
                    return;
                }

                if (_FileName == null)
                {
                    throw new Exception("Missing file name for " + GetType());
                }

                if (File.Exists(_FileName))
                {
                    try
                    {
                        _Value = JsonConvert.DeserializeObject<T>(File.ReadAllText(_FileName));
                        _Corrupt = false;
                    }
                    catch
                    {
                        InfrastructureTrace.Warning($"File corrupt: {_FileName}");
                        _Corrupt = true;
                    }
                }
            }

			if (_Value == null) {
				_IsNew = true;
				_Value = new T ();
			} else {
				_IsNew = false;
			}
		}

		public void Save() {
            var dir = Path.GetDirectoryName(_FileName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            lock (_sync)
            {
                File.WriteAllText(_FileName, JsonConvert.SerializeObject(_Value, Formatting.Indented));
            }

			_Corrupt = false;
			_IsNew = false;

			if (OnSaved != null)
				OnSaved();
		}

		public void Delete() {
			File.Delete (_FileName);
			_Value = null;
			_Corrupt = false;
			_IsNew = false;
		}
	}
}

