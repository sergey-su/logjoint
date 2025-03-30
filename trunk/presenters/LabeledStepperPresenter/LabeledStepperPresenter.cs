using System;
using System.Linq;

namespace LogJoint.UI.Presenters.LabeledStepperPresenter
{
    public class Presenter : IPresenter, IViewModel, IPresenterInternal
    {
        readonly IChangeNotification changeNotification;
        int currentValue;
        int[] allowedValues;
        int minValue = int.MinValue;
        int maxValue = int.MaxValue;
        bool enabled = true;

        public Presenter(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        public event EventHandler<EventArgs> OnValueChanged;

        int[] IPresenter.AllowedValues
        {
            get { return allowedValues; }
            set
            {
                allowedValues = value?.ToArray(); // protective copying
                EnforceConstraints();
                changeNotification.Post();
            }
        }

        int IPresenter.MinValue
        {
            get { return minValue; }
            set
            {
                minValue = value;
                EnforceConstraints();
                changeNotification.Post();
            }
        }

        int IPresenter.MaxValue
        {
            get { return maxValue; }
            set
            {
                maxValue = value;
                EnforceConstraints();
                changeNotification.Post();
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
                changeNotification.Post();
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
                changeNotification.Post();
            }
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        string IViewModel.Label => currentValue.ToString();

        bool IViewModel.EnabledUp => AllowedValuesSpecified ? enabled && UpAllowed : enabled;

        bool IViewModel.EnabledDown => AllowedValuesSpecified ? enabled && DownAllowed : enabled;

        bool IViewModel.EnabledLabel => enabled;

        void IViewModel.OnDownButtonClicked()
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
            changeNotification.Post();
        }

        void IViewModel.OnUpButtonClicked()
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
            changeNotification.Post();
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
            OnValueChanged?.Invoke(this, EventArgs.Empty);
        }
    };
};