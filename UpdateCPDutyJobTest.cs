namespace Utilities.Jobs.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Forte.Business.Entities;
    using Forte.Business.ServiceBase;

    using Utilities.JobEngine;
    using Utilities.JobEngine.Enums;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Rhino.Mocks;

    /// <summary>
    /// Holds the tests for UpdateCPDutyJob class
    /// </summary>
    [TestClass]
    public class UpdateCPDutyJobTest : JobRunnerTest
    {
        #region Methods

        /// <summary>
        /// Tests that the Duty changes are copied.
        /// </summary>
        [TestMethod]
        public void AcknowledgedDutyChangesCopiedBack()
        {
            // Arrange
            List<PairingDutyChange> dutyChanges = GetDutyChanges();

            IDataAccess dataAccess = GetMockDataAccess(dutyChanges);
            var sut = new UpdateCPDutyJob();
            var parameters = new List<JobParameter>
            {
                new JobParameter { Name = "ShowDutyHoursBefore",  Value = "1" },
                new JobParameter { Name = "ShowDutyHoursAfter",  Value = "1" },
            };

            // Act
            JobResult result = this.RunJob(sut, dataAccess, parameters.ToDictionary(jp => jp.Name));

            // Assert
            Assert.AreEqual(Result.Success, result.Result, this.JobOutput);

            IList<object[]> arguments = dataAccess.GetArgumentsForCallsMadeOn(x => x.Save<PairingDutyChangeEmployee>(null));
            Assert.AreEqual(1, arguments.Count, "Save should be called once.");
            Assert.IsNotNull(arguments[0][0]);
            var entities = arguments[0][0] as IEnumerable<PairingDutyChangeEmployee>;
            Assert.IsNotNull(entities);
            PairingDutyChangeEmployee entity = entities.FirstOrDefault();
            Assert.IsNotNull(entity);
            Assert.AreEqual(1, entity.AcknowledgedBy);
            Assert.AreEqual(2, entity.AcknowledgedType);
            Assert.AreEqual(new DateTime(2011, 1, 1), entity.AcknowledgedDate);
            Assert.AreEqual(2, entity.EmployeeId);
            dataAccess.VerifyAllExpectations();
        }

        /// <summary>
        /// Tests that the Duty changes are copied.
        /// </summary>
        [TestMethod]
        public void DutyChangesCopied()
        {
            // Arrange
            List<PairingDutyChange> dutyChanges = GetDutyChanges();

            IDataAccess dataAccess = GetMockDataAccess(dutyChanges);
            var sut = new UpdateCPDutyJob();

            // Act
            JobResult result = this.RunJob(sut, dataAccess);

            // Assert
            Assert.AreEqual(Result.Success, result.Result, this.JobOutput);

            IList<object[]> arguments = dataAccess.GetArgumentsForCallsMadeOn(x => x.Save<CPDutyChange>(null, false));
            var savedDutyChanges = (IEnumerable<CPDutyChange>)arguments[0][0];
            Assert.AreEqual(1, savedDutyChanges.Count());

            PairingDutyChange original = dutyChanges[0];
            CPDutyChange dutyChange = savedDutyChanges.First();
            
            Assert.AreEqual(original.Pairing.StartDate.ToString("d-MMM HH:mm"), dutyChange.DutySignOn);
            Assert.AreEqual(original.Pairing.EndDate.ToString("d-MMM HH:mm"), dutyChange.DutySignOff);
            Assert.AreEqual(original.Pairing.Label, dutyChange.Duty);
            Assert.AreEqual(original.Pairing.Port.IataCode + original.Pairing.ToPort.IataCode, dutyChange.DutyFromTo);
            Assert.AreEqual(1, dutyChange.EmployeeId);

            dataAccess.VerifyAllExpectations();
        }

        /// <summary>
        /// Tests that the old Duty changes are deleted.
        /// </summary>
        [TestMethod]
        public void OldDutyChangesDeleted()
        {
            // Arrange
            List<PairingDutyChange> dutyChanges = GetDutyChanges();

            IDataAccess dataAccess = GetMockDataAccess(dutyChanges);
            var sut = new UpdateCPDutyJob();

            // Act
            JobResult result = this.RunJob(sut, dataAccess);

            // Assert
            Assert.AreEqual(Result.Success, result.Result, this.JobOutput);

            // dataAccess.AssertWasCalled(x=>x.Delete(null), Arg)
            IList<object[]> arguments = dataAccess.GetArgumentsForCallsMadeOn(x => x.Delete<CPDutyChange>(null));
            Assert.IsNotNull(arguments[0][0]);

            dataAccess.VerifyAllExpectations();
        }

        /// <summary>
        /// Gets the duty changes.
        /// </summary>
        /// <returns>the duty changes.</returns>
        private static List<PairingDutyChange> GetDutyChanges()
        {
            DateTime startDate = new DateTime(2011, 1, 1);
            DateTime endDate = startDate.AddHours(10);
            Pairing pairing = new Pairing
                {
                    Id = 7,
                    StartDate = startDate,
                    EndDate = endDate,
                    Label = "XYZ",
                    Port = new Port { IataCode = "SYD" },
                    ToPort = new Port { IataCode = "BNE" }
                };

            List<PairingDutyChange> dutyChanges = new List<PairingDutyChange>
                {
                    new PairingDutyChange
                    {
                        Id = 5,
                        Pairing = pairing,
                        AlarmAt = startDate.AddDays(-4),
                        Notify = true,
                        HideUntil = startDate.AddDays(-5),
                        PublicComment = "Duty changed.",
                    }
                };

            dutyChanges[0].PairingDutyChangeEmployees.AddRange(
                   new List<PairingDutyChangeEmployee>
                   {
                        new PairingDutyChangeEmployee { Id = 1, EmployeeId = 1, PairingDutyChangeId = 5 },
                        new PairingDutyChangeEmployee { Id = 2, EmployeeId = 2, PairingDutyChangeId = 5 }
                   });

            return dutyChanges;
        }

        /// <summary>
        /// Gets the mock data access.
        /// </summary>
        /// <param name="dutyChanges">The duty changes.</param>
        /// <returns>Mock data access</returns>
        private static IDataAccess GetMockDataAccess(List<PairingDutyChange> dutyChanges)
        {
            IDataAccess dataAccess = MockRepository.GenerateMock<IDataAccess>();
            IEnumerable<PairingDutyChangeEmployee> dutyChangeEmployees;

            if (dutyChanges == null)
            {
                dutyChanges = new List<PairingDutyChange>();
                dutyChangeEmployees = new List<PairingDutyChangeEmployee>
                {
                    new PairingDutyChangeEmployee { EmployeeId = 3 }
                };
            }
            else
            {
                dutyChangeEmployees = dutyChanges[0].PairingDutyChangeEmployees;
            }

            var cpdutyChanges = new List<CPDutyChange>
            {
                new CPDutyChange { AcknowledgedBy = 1, AcknowledgedType = 2, AcknowledgedDate = new DateTime(2011, 1, 1), EmployeeId = 2, OriginalId = 5 }
            };

            dataAccess.Expect(x => x.Save<CPDutyChange>(null, false)).IgnoreArguments().Return(new List<CPDutyChange>()).Repeat.Any();
            dataAccess.Expect(x => x.Get<CPDuty>(null, null)).IgnoreArguments().Return(new List<CPDuty>()).Repeat.Any();
            dataAccess.Expect(x => x.Get<PairingDuty>(null, null)).IgnoreArguments().Return(new List<PairingDuty>()).Repeat.Any();
            dataAccess.Expect(x => x.GetAll<CompanyParameter>()).Return(new List<CompanyParameter>());
            return dataAccess;
        }

        #endregion Methods
    }
}