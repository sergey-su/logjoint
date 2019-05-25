﻿using System;
using Newtonsoft.Json;
using LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol;

namespace LogJoint.ORToolsWrapper
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var rq = (Request)JsonSerializer.Create().Deserialize(Console.In, typeof(Request));
			var rsp = Postprocessing.Correlation.EmbeddedSolver.OrToolsSolverCore.Solve(rq);
			JsonSerializer.Create(new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore
			}).Serialize(Console.Out, rsp);
		}
	}
}
