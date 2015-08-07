namespace Utilities.Jobs.Test
{
    using System.Collections.Generic;
    using System.Linq;

    using Forte.Business.Entities;
    using Forte.Business.ServiceBase;

    using Infrastructure.Logging;
    using Shared.Enums;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Rhino.Mocks;

    /// <summary>
    /// Holds tests for Job
    /// </summary>
    [TestClass]
    public class SendCPMessagesJobTest
    {
        #region Methods

        /// <summary>
        /// Tests that the CP messages job returns correct message type.
        /// </summary>
        [TestMethod]
        public void SendCPMessagesJobReturnsCorrectMessageType()
        {
            // Arrange
            var sut = new SendUserEmployeeMessageJob();

            // Act

            // Assert
            Assert.AreEqual(MessageTypeEnum.Portal, sut.MessageType);
        }

        /// <summary>
        /// Tests that the CP messages job saves messages.
        /// </summary>
        [TestMethod]
        public void SendCPMessagesJobSavesMessages()
        {
            // Arrange
            IDataAccess dataAccess = GetMockDataAccess();
            var sut = new SendUserEmployeeMessageJob();
            const string Content = "Test Content";
            var messageModel = new Message
            {
                Content = Content,
                Id = 1,
                Subject = "None",
            };

            messageModel.MessageNotifications.Add(new MessageNotification { EmployeeId = 5 });
            messageModel.MessageNotifications.Add(new MessageNotification { EmployeeId = 6 });
            sut.Initialise(new JobQueue(), dataAccess, new NullLogger(), new Dictionary<string, JobParameter>());

            // Act
            sut.SendMessage(messageModel);

            // Assert
            IList<object[]> arguments = dataAccess.GetArgumentsForCallsMadeOn(x => x.Save(Arg<IEnumerable<MessageEmployeeUser>>.Is.Anything));
            var savedMessages = (IEnumerable<MessageEmployeeUser>)arguments[0][0];
            Assert.AreEqual(2, savedMessages.Count());

            MessageEmployeeUser message = savedMessages.First();
            Assert.AreEqual(Content, message.Message);
            Assert.AreEqual(5, message.EmployeeId);

            message = savedMessages.Last();
            Assert.AreEqual(Content, message.Message);
            Assert.AreEqual(6, message.EmployeeId);
        }

        /// <summary>
        /// Gets the mock data access.
        /// </summary>
        /// <returns>Mock data access</returns>
        private static IDataAccess GetMockDataAccess()
        {
            var dataAccess = MockRepository.GenerateMock<IDataAccess>();

            dataAccess.Expect(x => x.Get<Employee>(e => e.Id == 5)).IgnoreArguments().Repeat.Once().Return(new List<Employee> { new Employee { Id = 5 } });
            dataAccess.Expect(x => x.Get<Employee>(e => e.Id == 6)).IgnoreArguments().Repeat.Once().Return(new List<Employee> { new Employee { Id = 6 } });
            dataAccess.Expect(x => x.Save(new List<MessageEmployeeUser>())).IgnoreArguments().Return(new List<MessageEmployeeUser>());
            return dataAccess;
        }

        #endregion Methods
    }
}