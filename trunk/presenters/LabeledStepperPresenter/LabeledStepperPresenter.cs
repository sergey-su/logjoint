using System;
using System.Linq;

namespace LogJoint.UI.Presenters.LabeledStepperPresenter
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		int currentValue;
		int[] allowedValues;
		int minValue = int.MinValue;
		int maxValue = int.MaxValue;
		bool enabled = true;

		public Presenter(
			IView view
		)
		{
			this.view = view;
			view.SetEventsHandler(this);
		}

		public event EventHandler<EventArgs> OnValueChanged;

		int [] IPresenter.AllowedValues 
		{ 
			get { return allowedValues; }
			set
			{
				allowedValues = value?.ToArray(); // protective copying
				EnforceConstraints();
				UpdateView();
			}
		}

		int IPresenter.MinValue
		{
			get { return minValue; }
			set
			{
				minValue = value;
				EnforceConstraints();
				UpdateView();
			}
		}
		              
		int IPresenter.MaxValue
		{
			get { return maxValue; }
			set
			{
				maxValue = value;
				EnforceConstraints();
				UpdateView();
			}
		}
		              
		int IPresenter.Value
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
		       
		bool IPresenter.Enabled
		{
			get { return enabled; }
			set
			{
				if (enabled == value)
					return;
				enabled = value;
				UpdateView();
			}
		}

		void IViewEvents.OnDownButtonClicked ()
		{
			if (AllowedValuesSpecified)
			{
				if (DownAllowed)
					currentValue = allowedValues.Last(allowedValue => allowedValue < currentValue);
			}
			else if (currentValue > minValue)
			{
				--currentValue;
			}
			FireValueChanged();
			UpdateView();
		}

		void IViewEvents.OnUpButtonClicked ()
		{
			if (AllowedValuesSpecified)
			{
				if (UpAllowed)
					currentValue = allowedValues.First(allowedValue => allowedValue > currentValue);
			}
			else if (currentValue < maxValue)
			{
				++currentValue;
			}
			FireValueChanged();
			UpdateView();
		}

		void UpdateView()
		{
			view.SetLabel(currentValue.ToString());
			if (AllowedValuesSpecified)
			{
				view.EnableControls(
					enableUp: enabled && UpAllowed, 
					enableDown: enabled && DownAllowed,
					enableLabel: enabled
				);
			}
			else
			{
				view.EnableControls(
					enableUp: enabled,
					enableDown: enabled,
					enableLabel: enabled
				);
			}
		}

		bool UpAllowed => currentValue != allowedValues.Last();
		bool DownAllowed => currentValue != allowedValues.First();
		bool AllowedValuesSpecified => allowedValues != null && allowedValues.Length > 0;

		void EnforceConstraints()
		{
			var valueCandidate = currentValue;
			if (AllowedValuesSpecified)
				valueCandidate = allowedValues.FirstOrDefault(allowedValue => allowedValue >= currentValue, allowedValues.Last());
			if (valueCandidate < minValue)
				valueCandidate = minValue;
			if (valueCandidate > maxValue)
				valueCandidate = maxValue;
			currentValue = valueCandidate;
			FireValueChanged();
		}

		void FireValueChanged()
		{
			OnValueChanged?.Invoke (this, EventArgs.Empty);
		}
	};
};