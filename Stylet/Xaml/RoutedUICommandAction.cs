using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Stylet.Xaml
{
	public class RoutedUICommandAction : RoutedUICommand
	{
		private static object GetActionTarget(DependencyObject obj)
		{
			return (object)obj.GetValue(ActionTargetProperty);
		}

		private static void SetActionTarget(DependencyObject obj, object value)
		{
			obj.SetValue(ActionTargetProperty, value);
		}

		// Using a DependencyProperty as the backing store for ActionTarget.  This enables animation, styling, binding, etc...
		private static readonly DependencyProperty ActionTargetProperty =
			DependencyProperty.RegisterAttached("ActionTarget", typeof(object), typeof(RoutedUICommandAction), new PropertyMetadata(default, OnActionTargetPropertyChanged));

		private static void OnActionTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SetMethods(d);
		}

		private static void SetMethods(DependencyObject d)
		{
			var target = GetTarget(d) ?? GetActionTarget(d);
			if (target == null)
			{
				return;
			}

			var targetType = target.GetType();
			var targetMethod = targetType.GetMethod(GetMethodName(d), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (targetMethod != null && targetMethod.ReturnType == typeof(void))
			{
				if (targetMethod.GetParameters().Length == 0)
				{
					SetExecute(d, o => targetMethod.Invoke(target, Array.Empty<object>()));
				}
				else
				{
					SetExecute(d, o => targetMethod.Invoke(target, new object[] { o }));
				}
			}

			var guardMethod = targetType.GetMethod(GetGuardName(d), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (guardMethod != null && guardMethod.ReturnType == typeof(bool))
			{
				if (guardMethod.GetParameters().Length == 0)
				{
					SetCanExecute(d, o => (bool)guardMethod.Invoke(target, Array.Empty<object>()));
				}
				else
				{
					SetCanExecute(d, o => (bool)guardMethod.Invoke(target, new object[] { o }));
				}

				return;
			}

			var guardProperty = targetType.GetProperty(GetGuardName(d), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (guardProperty != null && guardProperty.PropertyType == typeof(bool))
			{
				SetCanExecute(d, o => (bool)guardProperty.GetValue(target));
			}
			else
			{
				SetCanExecute(d, o => true);
			}
		}

		private static object GetTarget(DependencyObject obj)
		{
			return (object)obj.GetValue(TargetProperty);
		}

		private static void SetTarget(DependencyObject obj, object value)
		{
			obj.SetValue(TargetProperty, value);
		}

		// Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
		private static readonly DependencyProperty TargetProperty =
			DependencyProperty.RegisterAttached("Target", typeof(object), typeof(RoutedUICommandAction), new PropertyMetadata(default));


		private static string GetMethodName(DependencyObject obj)
		{
			return (string)obj.GetValue(MethodNameProperty);
		}

		private static void SetMethodName(DependencyObject obj, string value)
		{
			obj.SetValue(MethodNameProperty, value);
		}

		// Using a DependencyProperty as the backing store for MethodName.  This enables animation, styling, binding, etc...
		private static readonly DependencyProperty MethodNameProperty =
			DependencyProperty.RegisterAttached("MethodName", typeof(string), typeof(RoutedUICommandAction), new PropertyMetadata(default));



		private static string GetGuardName(DependencyObject obj)
		{
			return (string)obj.GetValue(GuardNameProperty);
		}

		private static void SetGuardName(DependencyObject obj, string value)
		{
			obj.SetValue(GuardNameProperty, value);
		}

		// Using a DependencyProperty as the backing store for GuardName.  This enables animation, styling, binding, etc...
		private static readonly DependencyProperty GuardNameProperty =
			DependencyProperty.RegisterAttached("GuardName", typeof(string), typeof(RoutedUICommandAction), new PropertyMetadata(default));



		private static Func<object, bool> GetCanExecute(DependencyObject obj)
		{
			return (Func<object, bool>)obj.GetValue(CanExecuteProperty);
		}

		private static void SetCanExecute(DependencyObject obj, Func<object, bool> value)
		{
			obj.SetValue(CanExecuteProperty, value);
		}

		// Using a DependencyProperty as the backing store for CanExecute.  This enables animation, styling, binding, etc...
		private static readonly DependencyProperty CanExecuteProperty =
			DependencyProperty.RegisterAttached("CanExecute", typeof(Func<object, bool>), typeof(RoutedUICommandAction), new PropertyMetadata(new Func<object,bool>(o => true)));



		private static Action<object> GetExecute(DependencyObject obj)
		{
			return (Action<object>)obj.GetValue(ExecuteProperty);
		}

		private static void SetExecute(DependencyObject obj, Action<object> value)
		{
			obj.SetValue(ExecuteProperty, value);
		}

		// Using a DependencyProperty as the backing store for Execute.  This enables animation, styling, binding, etc...
		private static readonly DependencyProperty ExecuteProperty =
			DependencyProperty.RegisterAttached("Execute", typeof(Action<object>), typeof(RoutedUICommandAction), new PropertyMetadata(default));

		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetElement"></param>
		/// <param name="target"></param>
		/// <param name="methodName"></param>
		/// <param name="commandNullTargetBehavior"></param>
		/// <param name="commandActionNotFoundBehavior"></param>
		/// <param name="dependencyProperty"></param>
		public RoutedUICommandAction(UIElement targetElement, object target, string methodName, ActionUnavailableBehaviour commandNullTargetBehavior, ActionUnavailableBehaviour commandActionNotFoundBehavior, DependencyProperty dependencyProperty) : base(methodName, methodName, typeof(RoutedUICommandAction))
		{
			SetTarget(targetElement, target);
			SetMethodName(targetElement, methodName);
			SetGuardName(targetElement, $"Can{methodName}");
			BindingOperations.ClearBinding(targetElement, ActionTargetProperty);
			BindingOperations.SetBinding(targetElement, ActionTargetProperty, new Binding()
			{
				Path = new PropertyPath(View.ActionTargetProperty),
				RelativeSource = new RelativeSource(RelativeSourceMode.Self)
			});
			foreach (CommandBinding commandBinding in targetElement.CommandBindings)
			{
				if (commandBinding.Command is RoutedUICommandAction routedUICommandAction && routedUICommandAction.Name == methodName)
				{
					targetElement.CommandBindings.Remove(commandBinding);
					break;
				}
			}

			targetElement.SetValue(dependencyProperty, this);
			targetElement.CommandBindings.Add(new CommandBinding(this, Execute, CanExecute));
		}

		private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Source is DependencyObject d)
			{
				e.CanExecute = GetCanExecute(d)?.Invoke(e.Parameter) ?? false;
			}
		}

		private void Execute(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Source is DependencyObject d)
			{
				GetExecute(d)?.Invoke(e.Parameter);
			}
		}
	}
}
