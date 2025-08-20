using System;
using System.Windows.Input;

namespace Imger.Utils
{
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
#pragma warning disable CS8767 // 参数类型中引用类型的为 Null 性与隐式实现的成员不匹配(可能是由于为 Null 性特性)。
        public bool CanExecute(object parameter) => true;
#pragma warning restore CS8767 // 参数类型中引用类型的为 Null 性与隐式实现的成员不匹配(可能是由于为 Null 性特性)。

#pragma warning disable CS8767 // 参数类型中引用类型的为 Null 性与隐式实现的成员不匹配(可能是由于为 Null 性特性)。
        public void Execute(object parameter) => _execute(parameter);
#pragma warning restore CS8767 // 参数类型中引用类型的为 Null 性与隐式实现的成员不匹配(可能是由于为 Null 性特性)。

#pragma warning disable CS8612 // 类型中引用类型的为 Null 性与隐式实现的成员不匹配。
        public event EventHandler CanExecuteChanged { add { } remove { } }
#pragma warning restore CS8612 // 类型中引用类型的为 Null 性与隐式实现的成员不匹配。
    }
}
