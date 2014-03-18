using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace UnfuckUpFileNames.UiInfrastructure
{
	public class CommandBase : ICommand
	{
		private Action _execute;
		private Func<bool> _canExecute;

		public CommandBase(Action execute)
		{
			this._execute = execute;
		}

		public CommandBase(Action execute, Func<bool> canExecute)
			: this(execute)
		{
			this._canExecute = canExecute;
		}

		public virtual void RaiseCanExecute()
		{
			bool canExecute = this.CanExecute(null);

			if (CanExecuteChanged != null)
			{
				this.CanExecuteChanged(this, null);
			}
		}

		public virtual bool CanExecute(object parameter)
		{
			if (this._canExecute != null)
			{
				return this._canExecute.Invoke();
			}

			return true;
		}

		public event EventHandler CanExecuteChanged;

		public virtual void Execute(object parameter)
		{
			if (this.CanExecute(parameter) && this._execute != null)
			{
				this._execute.Invoke();
			}
		}
	}
}
