using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.SharingDialog
{
	public interface IView
	{
		void SetEventsHandler(IViewEvents presenter);
		void Show();
		void UpdateDescription(string value);
		void UpdateWorkspaceUrlEditBox(string value, bool isHintValue, bool allowCopying);
		void UpdateDialogButtons(bool uploadEnabled, string uploadText, string cancelText);
		void UpdateProgressIndicator(string text, bool isError, string details);
		string GetWorkspaceNameEditValue();
		string GetWorkspaceAnnotationEditValue();
		void UpdateWorkspaceEditControls(bool enabled, string nameValue, string nameBanner, string nameWarning, string annotationValue);
	};

	public interface IPresenter
	{
		DialogAvailability Availability { get; }
		void ShowDialog();

		event EventHandler AvailabilityChanged;
	};

	public enum DialogAvailability
	{
		PermanentlyUnavaliable,
		TemporarilyUnavailable,
		Available
	};

	public interface IViewEvents
	{
		void OnUploadButtonClicked();
	};
};