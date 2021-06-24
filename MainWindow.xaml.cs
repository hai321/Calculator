using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
namespace Calculator
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Data;

    public partial class MainWindow : Window
    {
        private readonly DataTable _dt;
        private CalculatorState _state;
        private readonly LabelManager _currentExpression;
        private readonly LabelManager _expressionManager;
        private readonly LabelManager _operandManager;
        private readonly Stack<string> _expressions;

        public MainWindow()
        {
            InitializeComponent();

            _dt = new DataTable();

            _state = CalculatorState.Clear;

            _expressions = new Stack<string>();

            _currentExpression = new LabelManager();
            _currentExpression.ClearValue();

            _expressionManager = new LabelManager(totalResultLabel, false);
            _expressionManager.ClearValue();

            _operandManager = new LabelManager(resLabel);
            _operandManager.ClearValue();
        }

        /// <summary>
        /// Clears the current expression and the label if the result label is empty then it clears the whole result
        /// </summary>
        private void CeButton_Click(object sender, RoutedEventArgs e)
        {
            _state = _state == CalculatorState.ClearExpression || _state == CalculatorState.Total ? CalculatorState.Clear : CalculatorState.ClearExpression;

            if (_state == CalculatorState.Clear)
            {
                _currentExpression.ClearValue();
                _expressionManager.ClearValue();
            }

            _operandManager.ClearValue();
        }

        /// <summary>
        /// 
        /// </summary>
        private void BackspaceButtonClick(object sender, RoutedEventArgs e)
        {
            // Reset the entire calculation
            if (_state == CalculatorState.Clear)
            {
                _currentExpression.ClearValue();
                _expressionManager.ClearValue();
                return;
            }

            // Backspace is not relevant if no operand was used
            if (_state != CalculatorState.Operand)
            {
                return;
            }

            _operandManager.RemoveLastChar();
        }

        /// <summary>
        /// '(' or ')' was clicked, pushes and pops the stack with the relvant expression
        /// </summary>
        private void BracketsClick(object sender, RoutedEventArgs e)
        {
            string currentBracket = ((Button)sender).Content.ToString();
            if (currentBracket == "(")
            {
                _expressions.Push(_currentExpression.GetValue());
                _currentExpression.ClearValue();
                _operandManager.SetValue("0",true);
                _expressionManager.AddChar("(");
            }
            else if (currentBracket == ")" && _expressions.Count > 0) //opening brackets must be larger than the closing ones
            {
                string lastExpression = _expressions.Pop();

                // Calculate current inner expression
                string currentResult = CalculateResult();

                _currentExpression.SetValue($"{lastExpression}{currentResult}");

                // Clear the current inner expression
                _operandManager.ClearValue(remainDisplay: true);

                _expressionManager.AddChar(")");
            }
        }

        /// <summary>
        /// (1-9, PI, e) was clicked
        /// </summary>
        private void OperandClick(object sender, RoutedEventArgs e)
        {
            string operandName = ((Button)sender).Name;

            // Start new calculation if state is clear or done calculation
            if (_state == CalculatorState.Clear || _state == CalculatorState.Total)
            {
                _currentExpression.ClearValue();
                _expressionManager.ClearValue();
            }

            // Overwrite any temporary number
            if (_state != CalculatorState.Operand)
            {
                _operandManager.ClearValue();
            }

            _state = CalculatorState.Operand;

            // The next 2 if statements are for the constants
            if (operandName == "piButton")
            {
                _operandManager.SetValue(Math.PI.ToString());
                return;
            }
            if (operandName == "eButton")
            {
                _operandManager.SetValue(Math.E.ToString());
                return;
            }

            // The code below is for digits (0-9)
            string currentOperand = ((Button)sender).Content.ToString();
            _operandManager.AddChar(currentOperand);
        }

        /// <summary>
        /// One of the operator was clicked (+,-,/,*)
        /// </summary>
        private void OperatorClick(object sender, RoutedEventArgs e)
        {
            string operatorValue = ((Button)sender).Content.ToString();

            // Change the last used operator value
            if (_state == CalculatorState.Operator)
            {
                _currentExpression.ReplaceLastChar(operatorValue);
                _expressionManager.ReplaceLastChar(operatorValue);
            }
            else
            {
                if (_state == CalculatorState.Function)
                {
                    _operandManager.ClearValue();
                }

                string operandValue = _operandManager.GetValueAndClearWithDisplay();
                _currentExpression.SetValue($"{_currentExpression.GetValue()}{operandValue}{operatorValue}");
                _expressionManager.SetValue($"{_expressionManager.GetValue()}{operandValue}{operatorValue}");
            }

            _state = CalculatorState.Operator;

            CalculateResult();
        }

        /// <summary>
        /// Shows the result and clears all the previous calculations
        /// </summary>
        private void EqualClick(object sender, RoutedEventArgs e)
        {
            if (_state == CalculatorState.Function)
            {
                _operandManager.ClearValue();
            }

            _state = CalculatorState.Total;
            CalculateResult();

        }

        /// <summary>
        /// Handles inverse/absolute value/*-1/factorial functions
        /// </summary>
        private void MathematicalFunction(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Name;
            string currentResult = _operandManager.GetValueAndClearWithDisplay();

            // Handle situation of applying more then one function on same operand over and over
            if (_state == CalculatorState.Function)
            {
                string temp = _currentExpression.GetValue();
                _currentExpression.SetValue($"{temp.Substring(0, temp.Length - currentResult.Length)}");

                temp = _expressionManager.GetValue();
                _expressionManager.SetValue($"{temp.Substring(0, temp.Length - currentResult.Length)}");
            }

            // The final result that will be applied
            string calculationResult = string.Empty;
            if (!double.TryParse(currentResult, out double parsedResult))
            {
                return;
            }

            switch (buttonName)
            {
                case "inverseButton":
                    calculationResult = (1 / parsedResult).ToString();
                    break;

                case "absButton":
                    calculationResult = Math.Abs(parsedResult).ToString();
                    break;

                case "negativeButton":
                    parsedResult *= -1;
                    calculationResult = parsedResult.ToString();
                    break;

                case "factorialButton":
                    int n = (int)Math.Floor(parsedResult);

                    // In case the input is a negative number
                    try
                    {
                        calculationResult = MathNet.Numerics.SpecialFunctions.Factorial(n).ToString();
                        if (calculationResult == "∞")
                        {
                            _operandManager.SetValue("Overflow");
                            _state = CalculatorState.Clear;
                            return;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        _operandManager.SetValue("Invalid input");
                        _state = CalculatorState.Clear;
                        return;
                    }

                    break;
            }

            _operandManager.SetValue(calculationResult);
            _currentExpression.SetValue($"{_currentExpression.GetValue()}{calculationResult}");
            _expressionManager.SetValue($"{_expressionManager.GetValue()}{calculationResult}");

            _state = CalculatorState.Function;
        }

        /// <summary>
        /// Calculates the current expression for the result label
        /// </summary>
        private string CalculateResult()
        {
            _currentExpression.SetValue($"{_currentExpression.GetValue()}{_operandManager.GetValue()}");
            _expressionManager.SetValue($"{_expressionManager.GetValue()}{_operandManager.GetValue()}");
            string expression = _currentExpression.GetValue();

            switch (_state)
            {
                case CalculatorState.Total:
                    string closedBrackets = new string(')', _expressions.Count);
                    expression = $"{_expressionManager.GetValue()}{closedBrackets}";
                    _expressionManager.SetValue($"{expression}=", displayOnly: true);
                    _currentExpression.ClearValue();
                    break;

                case CalculatorState.Operator:
                    // If an operator was clicked take the expression before that operator(i.e "4+3+" -> "4+3")
                    expression = expression.Substring(0, expression.Length - 1);
                    break;
            }

            // Computes the current expression using DataTable.Compute method
            try
            {
                string calculationResult = _dt.Compute(expression, "").ToString();


                if (calculationResult == "∞")
                {
                    _operandManager.SetValue("Can't divide 0");
                    _state = CalculatorState.Clear;
                    return string.Empty;
                }

                _operandManager.SetValue(calculationResult, displayOnly: _state != CalculatorState.Total);

                return calculationResult;
            }
            catch (SyntaxErrorException)
            {
                _operandManager.SetValue("Invalid input");
                _state = CalculatorState.Clear;
                return string.Empty;
            }
        }

        /// <summary>
        /// Current calculator state based on user actions
        /// </summary>
        private enum CalculatorState
        {
            Operand,
            Operator,
            Function,
            ClearExpression,
            Clear,
            Total
        }

        /// <summary>
        /// Manages both internal text storage and UI label for display
        /// </summary>
        private class LabelManager
        {
            private readonly StringBuilder _value;

            private readonly Label _label;

            private readonly bool _setToZeroIfEmpty;

            public LabelManager(Label label = null, bool setToZeroIfEmpty = true)
            {
                _value = new StringBuilder();
                _label = label;
                _setToZeroIfEmpty = setToZeroIfEmpty;
            }

            public string GetValue()
            {
                return _value.ToString();
            }

            public void SetValue(string newValue, bool displayOnly = false)
            {
                ClearValue();
                if (displayOnly)
                {
                    SetLabelValue(newValue);
                }
                else
                {
                    _value.Append(newValue);
                    SetLabelValue(_value.ToString());
                }
            }

            private void SetLabelValue(string value)
            {
                if (_label == null)
                {
                    return;
                }

                _label.Content = value;
            }

            public void ClearValue(bool remainDisplay = false)
            {
                _value.Clear();
                if (!remainDisplay)
                {
                    SetLabelValue(_setToZeroIfEmpty ? "0" : string.Empty);
                }
            }

            public string GetValueAndClearWithDisplay()
            {
                string value = GetValue();
                ClearValue(remainDisplay: true);
                return value;
            }

            public void AddChar(string ch)
            {
                _value.Append(ch);
                SetLabelValue(_value.ToString());
            }

            public void RemoveLastChar()
            {
                if (_value.Length == 0)
                {
                    return;
                }

                // If the result label length is 1 negative digit(i.e "-2") or a single positive digit, change the result to 0
                if (_value[0] == '-' && _value.Length == 2)
                {
                    ClearValue();
                }
                else
                {
                    _value.Length--;
                    SetLabelValue(_value.ToString());
                }

                if (_value.Length == 0)
                {
                    ClearValue();
                }
            }

            public void ReplaceLastChar(string operand)
            {
                RemoveLastChar();
                AddChar(operand);
            }
        }
    }
}
