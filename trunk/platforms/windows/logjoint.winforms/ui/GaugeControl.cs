using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class GaugeControl : UserControl
	{
		int currentValue;
		int[] allowedValues;
		int minValue = int.MinValue;
		int maxValue = int.MaxValue;

		public GaugeControl()
		{
			InitializeComponent();
		}

		public int[] AllowedValues
		{
			get { return allowedValues; }
			set
			{
				allowedValues = value;
				EnforceConstraints();
				UpdateView();
			}
		}

		public int MinValue
		{
			get { return minValue; }
			set
			{
				minValue = value;
				EnforceConstraints();
				UpdateView();
			}
		}

		public int MaxValue
		{
			get { return maxValue; }
			set
			{
				maxValue = value;
				EnforceConstraints();
				UpdateView();
			}
		}

		public int Value
		{
			get
			{
				return currentValue;
			}
			set
			{
				currentValue = value;
				EnforceConstraints();
				UpdateView();
			}
		}

		void UpdateView()
		{
			valueLabel.Text = currentValue.ToString();
			if (AllowedValuesSpecified())
			{
				upButton.Enabled = currentValue != allowedValues.Last();
				downButton.Enabled = currentValue != allowedValues.First();
			}
			else
			{
				upButton.Enabled = true;
				downButton.Enabled = true;
			}
		}

		void EnforceConstraints()
		{
			var valueCandidate = currentValue;
			if (AllowedValuesSpecified())
				valueCandidate = allowedValues.First(allowedValue => allowedValue >= currentValue);
			if (valueCandidate < minValue)
				valueCandidate = minValue;
			if (valueCandidate > maxValue)
				valueCandidate = maxValue;
			currentValue = valueCandidate;
		}

		bool AllowedValuesSpecified()
		{
			return allowedValues != null && allowedValues.Length > 0;
		}

		private void upButton_Click(object sender, EventArgs e)
		{
			if (AllowedValuesSpecified())
				currentValue = allowedValues.First(allowedValue => allowedValue > currentValue);
			else if (currentValue < maxValue)
				++currentValue;
			UpdateView();
		}

		private void downButton_Click(object sender, EventArgs e)
		{
			if (AllowedValuesSpecified())
				currentValue = allowedValues.Last(allowedValue => allowedValue < currentValue);
			else if (currentValue > minValue)
				--currentValue;
			UpdateView();
		}
	}
}
