using LogJoint;
using System;
using System.Reflection;
using EM = LogJoint.Tests.ExpectedMessage;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture()]
	public class OracleFormatTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(), 
				"Oracle", "11g alert log xml");
		}

		void DoTest(string testLog, ExpectedLog expectedLog)
		{
			ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		void DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			DoTest(testLog, (new ExpectedLog()).Add(0, expectedMessages));
		}

		[Test]
		public void SmokeTest()
		{
			DoTest(@"
<msg time='2010-03-30T01:00:00.107-05:00' org_id='oracle' comp_id='rdbms'
client_id='' type='UNKNOWN' level='16'
host_id='RPRO9' host_addr='151.1.0.76' module=''
pid='3900'>
<txt>Setting Resource Manager plan DEFAULT_MAINTENANCE_PLAN via parameter
</txt>
</msg>
<msg time='2010-03-30T01:00:10.254-05:00' org_id='oracle' comp_id='rdbms'
client_id='' type='UNKNOWN' level='16'
host_id='RPRO9' host_addr='151.1.0.76' module=''
pid='4704'>
<txt>Thread 1 advanced to log sequence 40 (LGWR switch)
</txt>
</msg>",
				new EM(
@"Setting Resource Manager plan DEFAULT_MAINTENANCE_PLAN via parameter
  Organization ID: oracle
  Component ID: rdbms
  Client ID: 
  Process ID: 3900
  Type: UNKNOWN
  Host ID: RPRO9
  Host addr: 151.1.0.76
  Module:", null,
			       	new DateTime(2010, 03, 30, 06, 00, 00, 107, DateTimeKind.Utc)) { TextNeedsNormalization = true },
				new EM(null, null,
					new DateTime(2010, 03, 30, 06, 00, 10, 254, DateTimeKind.Utc)) 
					{ TextVerifier = t => t.StartsWith("Thread 1 advanced to log sequence 40 (LGWR switch)") }
			);
		}

	}
}
