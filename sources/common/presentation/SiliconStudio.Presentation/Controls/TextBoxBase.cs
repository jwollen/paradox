using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using SiliconStudio.Presentation.Core;

namespace SiliconStudio.Presentation.Controls
{
    /// <summary>
    /// An implementation of the <see cref="System.Windows.Controls.TextBox"/> control
    /// that provides additional features such as a proper validation/cancellation workflow.
    /// </summary>
    public class TextBoxBase : System.Windows.Controls.TextBox
    {
        private bool validating;

        /// <summary>
        /// Identifies the <see cref="HasText"/> dependency property.
        /// </summary>
        private static readonly DependencyPropertyKey HasTextPropertyKey = DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="GetFocusOnLoad"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GetFocusOnLoadProperty = DependencyProperty.Register("GetFocusOnLoad", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="SelectAllOnFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectAllOnFocusProperty = DependencyProperty.Register("SelectAllOnFocus", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="ValidateWithEnter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidateWithEnterProperty = DependencyProperty.Register("ValidateWithEnter", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ValidateOnTextChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidateOnTextChangeProperty = DependencyProperty.Register("ValidateOnTextChange", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="ValidateOnLostFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidateOnLostFocusProperty = DependencyProperty.Register("ValidateOnLostFocus", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(true, OnLostFocusActionChanged));

        /// <summary>
        /// Identifies the <see cref="CancelWithEscape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelWithEscapeProperty = DependencyProperty.Register("CancelWithEscape", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CancelOnLostFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelOnLostFocusProperty = DependencyProperty.Register("CancelOnLostFocus", typeof(bool), typeof(TextBoxBase), new PropertyMetadata(false, OnLostFocusActionChanged));

        /// <summary>
        /// Identifies the <see cref="ValidateCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidateCommandProperty = DependencyProperty.Register("ValidateCommand", typeof(ICommand), typeof(TextBoxBase));

        /// <summary>
        /// Identifies the <see cref="ValidateCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidateCommandParameterProprty = DependencyProperty.Register("ValidateCommandParameter", typeof(object), typeof(TextBoxBase));

        /// <summary>
        /// Identifies the <see cref="CancelCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.Register("CancelCommand", typeof(ICommand), typeof(TextBoxBase));

        /// <summary>
        /// Identifies the <see cref="CancelCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CancelCommandParameterProprty = DependencyProperty.Register("CancelCommandParameter", typeof(object), typeof(TextBoxBase));

        /// <summary>
        /// Raised just before the TextBox changes are validated. This event is cancellable
        /// </summary>
        public static readonly RoutedEvent ValidatingEvent = EventManager.RegisterRoutedEvent("Validating", RoutingStrategy.Bubble, typeof(CancelRoutedEventHandler), typeof(TextBox));

        /// <summary>
        /// Raised when TextBox changes have been validated.
        /// </summary>
        public static readonly RoutedEvent ValidatedEvent = EventManager.RegisterRoutedEvent("Validated", RoutingStrategy.Bubble, typeof(ValidationRoutedEventHandler<string>), typeof(TextBox));

        /// <summary>
        /// Raised when the TextBox changes are cancelled.
        /// </summary>
        public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent("Cancelled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TextBox));

        static TextBoxBase()
        {
            TextProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnTextChanged, null, true, UpdateSourceTrigger.Explicit));
        }

        public TextBoxBase()
        {
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Gets whether this TextBox contains a non-empty text.
        /// </summary>
        public bool HasText { get { return (bool)GetValue(HasTextPropertyKey.DependencyProperty); } private set { SetValue(HasTextPropertyKey, value); } }

        /// <summary>
        /// Gets or sets whether the associated text box should get keyboard focus when this behavior is attached.
        /// </summary>
        public bool GetFocusOnLoad { get { return (bool)GetValue(GetFocusOnLoadProperty); } set { SetValue(GetFocusOnLoadProperty, value); } }

        /// <summary>
        /// Gets or sets whether the text of the TextBox must be selected when the control gets focus.
        /// </summary>
        public bool SelectAllOnFocus { get { return (bool)GetValue(SelectAllOnFocusProperty); } set { SetValue(SelectAllOnFocusProperty, value); } }

        /// <summary>
        /// Gets or sets whether the validation should happen when the user press <b>Enter</b>.
        /// </summary>
        public bool ValidateWithEnter { get { return (bool)GetValue(ValidateWithEnterProperty); } set { SetValue(ValidateWithEnterProperty, value); } }

        /// <summary>
        /// Gets or sets whether the validation should happen as soon as the <see cref="TextBox.Text"/> is changed.
        /// </summary>
        public bool ValidateOnTextChange { get { return (bool)GetValue(ValidateOnTextChangeProperty); } set { SetValue(ValidateOnTextChangeProperty, value); } }

        /// <summary>
        /// Gets or sets whether the validation should happen when the control losts focus.
        /// </summary>
        public bool ValidateOnLostFocus { get { return (bool)GetValue(ValidateOnLostFocusProperty); } set { SetValue(ValidateOnLostFocusProperty, value); } }

        /// <summary>
        /// Gets or sets whether the cancellation should happen when the user press <b>Escape</b>.
        /// </summary>
        public bool CancelWithEscape { get { return (bool)GetValue(CancelWithEscapeProperty); } set { SetValue(CancelWithEscapeProperty, value); } }

        /// <summary>
        /// Gets or sets whether the cancellation should happen when the control losts focus.
        /// </summary>
        public bool CancelOnLostFocus { get { return (bool)GetValue(CancelOnLostFocusProperty); } set { SetValue(CancelOnLostFocusProperty, value); } }

        /// <summary>
        /// Gets or sets the command to execute when the validation occurs.
        /// </summary>
        public ICommand ValidateCommand { get { return (ICommand)GetValue(ValidateCommandProperty); } set { SetValue(ValidateCommandProperty, value); } }

        /// <summary>
        /// Gets or sets the parameter of the command to execute when the validation occurs.
        /// </summary>
        public object ValidateCommandParameter { get { return GetValue(ValidateCommandParameterProprty); } set { SetValue(ValidateCommandParameterProprty, value); } }

        /// <summary>
        /// Gets or sets the command to execute when the cancellation occurs.
        /// </summary>
        public ICommand CancelCommand { get { return (ICommand)GetValue(CancelCommandProperty); } set { SetValue(CancelCommandProperty, value); } }

        /// <summary>
        /// Gets or sets the parameter of the command to execute when the cancellation occurs.
        /// </summary>
        public object CancelCommandParameter { get { return GetValue(CancelCommandParameterProprty); } set { SetValue(CancelCommandParameterProprty, value); } }

        /// <summary>
        /// Raised just before the TextBox changes are validated. This event is cancellable
        /// </summary>
        public event CancelRoutedEventHandler Validating { add { AddHandler(ValidatingEvent, value); } remove { RemoveHandler(ValidatingEvent, value); } }

        /// <summary>
        /// Raised when TextBox changes have been validated.
        /// </summary>
        public event ValidationRoutedEventHandler<string> Validated { add { AddHandler(ValidatedEvent, value); } remove { RemoveHandler(ValidatedEvent, value); } }

        /// <summary>
        /// Raised when the TextBox changes are cancelled.
        /// </summary>
        public event RoutedEventHandler Cancelled { add { AddHandler(CancelledEvent, value); } remove { RemoveHandler(CancelledEvent, value); } }

        /// <summary>
        /// Validates the current changes in the TextBox.
        /// </summary>
        public void Validate()
        {
            var cancelRoutedEventArgs = new CancelRoutedEventArgs(ValidatingEvent);
            OnValidating(cancelRoutedEventArgs);
            if (cancelRoutedEventArgs.Cancel)
                return;

            RaiseEvent(cancelRoutedEventArgs);
            if (cancelRoutedEventArgs.Cancel)
                return;

            validating = true;
            var coercedText = CoerceTextForValidation(Text);
            SetCurrentValue(TextProperty, coercedText);

            BindingExpression expression = GetBindingExpression(TextProperty);
            if (expression != null)
                expression.UpdateSource();

            ClearUndoStack();

            var validatedArgs = new ValidationRoutedEventArgs<string>(ValidatedEvent, coercedText);
            OnValidated();

            RaiseEvent(validatedArgs);
            if (ValidateCommand != null && ValidateCommand.CanExecute(ValidateCommandParameter))
                ValidateCommand.Execute(ValidateCommandParameter);
            validating = false;
        }

        /// <summary>
        /// Cancels the current changes in the TextBox.
        /// </summary>
        public void Cancel()
        {
            BindingExpression expression = GetBindingExpression(TextProperty);
            if (expression != null)
                expression.UpdateTarget();

            ClearUndoStack();

            var cancelledArgs = new RoutedEventArgs(CancelledEvent);
            OnCancelled();
            RaiseEvent(cancelledArgs);

            if (CancelCommand != null && CancelCommand.CanExecute(CancelCommandParameter))
                CancelCommand.Execute(CancelCommandParameter);
        }

        /// <summary>
        /// Raised when the text of the TextBox changes.
        /// </summary>
        /// <param name="oldValue">The old value of the <see cref="TextBox.Text"/> property.</param>
        /// <param name="newValue">The new value of the <see cref="TextBox.Text"/> property.</param>
        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
        }

        /// <summary>
        /// Raised when the text of the TextBox is being validated.
        /// </summary>
        /// <param name="e">The event argument.</param>
        protected virtual void OnValidating(CancelRoutedEventArgs e)
        {
        }

        /// <summary>
        /// Raised when the current changes have has been validated.
        /// </summary>
        protected virtual void OnValidated()
        {
        }

        /// <summary>
        /// Raised when the current changes have been cancelled.
        /// </summary>
        protected virtual void OnCancelled()
        {
        }

        /// <summary>
        /// Coerces the text during the validation process. This method is invoked by <see cref="Validate"/>.
        /// </summary>
        /// <param name="baseValue">The value to coerce.</param>
        /// <returns>The coerced value.</returns>
        protected virtual string CoerceTextForValidation(string baseValue)
        {
            return MaxLength > 0 && baseValue.Length > MaxLength ? baseValue.Substring(0, MaxLength) : baseValue;
        }

        /// <inheritdoc/>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && ValidateWithEnter)
            {
                Validate();
            }
            if (e.Key == Key.Escape && CancelWithEscape)
            {
                Cancel();
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            if (SelectAllOnFocus)
            {
                SelectAll();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                if (SelectAllOnFocus)
                {
                    // We handle the event only when the SelectAllOnFocus property is active. If we don't handle it, base.OnMouseDown will clear the selection
                    // we're just about to do. But if we handle it, the caret won't be moved to the cursor position, which is the behavior we expect when SelectAllOnFocus is inactive.
                    e.Handled = true;
                }
                Focus();
            }
            base.OnMouseDown(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (ValidateOnLostFocus)
            {
                Validate();
            }
            if (CancelOnLostFocus)
            {
                Cancel();
            }

            base.OnLostKeyboardFocus(e);
        }

        private void ClearUndoStack()
        {
            var limit = UndoLimit;
            UndoLimit = 0;
            UndoLimit = limit;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (GetFocusOnLoad)
            {
                Keyboard.Focus(this);
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextBoxBase)d;
            input.HasText = e.NewValue != null && ((string)e.NewValue).Length > 0;
            input.OnTextChanged((string)e.OldValue, (string)e.NewValue);
            if (input.ValidateOnTextChange && !input.validating)
                input.Validate();
        }

        private static void OnLostFocusActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var input = (TextBoxBase)d;
            if (e.Property == ValidateOnLostFocusProperty && (bool)e.NewValue)
            {
                input.SetCurrentValue(CancelOnLostFocusProperty, false);
            }
            if (e.Property == CancelOnLostFocusProperty && (bool)e.NewValue)
            {
                input.SetCurrentValue(ValidateOnLostFocusProperty, false);
            }
        }
    }
}