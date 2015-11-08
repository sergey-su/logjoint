using System;
using System.Collections.Generic;
using System.Text;
using MonoMac.AppKit;

namespace LogJoint.Extensibility
{
	public interface IMainFormTabExtension
	{
		NSView PageControl { get; }
		string Caption { get; }
		void OnTabPageSelected();
	};
}