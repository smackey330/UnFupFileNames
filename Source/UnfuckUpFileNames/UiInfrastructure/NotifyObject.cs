using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnfuckUpFileNames.UiInfrastructure
{
	public class NotifyObject : INotifyPropertyChanged, IDisposable
	{
		private readonly Dictionary<string, PropertyChangedEventArgs> _eventArgCache = new Dictionary<string, PropertyChangedEventArgs>();

		public event PropertyChangedEventHandler PropertyChanged;

		public PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
		{
			bool isCached = this._eventArgCache.ContainsKey(propertyName);
			if (!isCached)
			{
				this._eventArgCache.Add(
					propertyName,
					new PropertyChangedEventArgs(propertyName));
			}

			PropertyChangedEventArgs args = this._eventArgCache[propertyName];
			return args;
		}

		public virtual void Dispose()
		{
			this._eventArgCache.Clear();
			this.PropertyChanged = null;
		}

		#region Protected Members

		protected virtual void AfterPropertyChanged(string propertyName)
		{
		}

		protected void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = this.PropertyChanged;
			if (handler != null)
			{
				PropertyChangedEventArgs args =
					this.GetPropertyChangedEventArgs(propertyName);

				handler(this, args);
			}

			this.AfterPropertyChanged(propertyName);
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, e);
			}
		}

		#endregion
	}
}
