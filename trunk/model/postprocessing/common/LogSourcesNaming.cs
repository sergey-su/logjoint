using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
	/// <summary>
	/// The class combines names that can be given to the source of particular log.
	/// The names are user-friendly. They can be presented to the user.
	/// </summary>
	public class LogSourceNames
	{
		/// <summary>
		/// Name of the business role that the log producer plays. For example if the log in question is from a service
		/// the value contains user-fiendly name of the whole service such as "Invoices service".
		/// </summary>
		public string RoleName;
		/// <summary>
		/// If the log producer runs on multiple machines (instances) this field conatins the user-friendly name of the instance.
		/// Example: "InvoicesService.2"
		/// </summary>
		public string RoleInstanceName;
	}

	/// <summary>
	/// Provides user-fieldny log sources' names to postprocessors views
	/// </summary>
	public interface ILogSourceNamesProvider
	{
		ILogSourceNamesGenerator CreateNamesGenerator();
	};

	public interface ILogSourceNamesGenerator: IDisposable
	{
		LogSourceNames Generate(ILogSource logSource);
	};

	public interface IAggregatingLogSourceNamesProvider: ILogSourceNamesProvider
	{
		void RegisterInnerProvider(ILogSourceNamesProvider logSourceNamesProvider);
	};

	public class AggregatingLogSourceNamesProvider : IAggregatingLogSourceNamesProvider
	{
		readonly HashSet<ILogSourceNamesProvider> inner = new HashSet<ILogSourceNamesProvider>();

		ILogSourceNamesGenerator ILogSourceNamesProvider.CreateNamesGenerator()
		{
			return new Generator() { inner = this.inner.Select(i => i.CreateNamesGenerator()).ToArray() };
		}

		void IAggregatingLogSourceNamesProvider.RegisterInnerProvider(ILogSourceNamesProvider logSourceNamesProvider)
		{
			inner.Add(logSourceNamesProvider);
		}

		class Generator : ILogSourceNamesGenerator
		{
			internal ILogSourceNamesGenerator[] inner;

			void IDisposable.Dispose()
			{
			}

			LogSourceNames ILogSourceNamesGenerator.Generate(ILogSource logSource)
			{
				return inner.Select(i => i.Generate(logSource)).FirstOrDefault(i => i != null);
			}
		};
	};
}
