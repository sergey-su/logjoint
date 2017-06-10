using System;
using System.Collections.Generic;
using System.Text;
using AppKit;

namespace LogJoint.Extensibility
{
	public interface IMainFormTabExtension
	{
		NSView PageControl { get; }
		string Caption { get; }
		void OnTabPageSelected();
	};
}