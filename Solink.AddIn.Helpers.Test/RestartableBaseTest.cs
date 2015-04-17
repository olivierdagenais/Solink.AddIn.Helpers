using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Solink.AddIn.Helpers.Test
{
    public interface IThing
    {
        int ComputeAnswerToLifeAndUniverseEverything();
        void AddToList(IList<string> strings);
        int Id { get; }
    }

    /// <summary>
    /// A class to test <see cref="RestartableBase{T,TException}"/>.
    /// </summary>
    [TestClass]
    public class RestartableBaseTest
    {
        private class RestartableThing : RestartableBase<IThing, SecurityException>, IThing
        {
            public RestartableThing(Func<IThing> factory) : base(factory)
            {
            }

            public int ComputeAnswerToLifeAndUniverseEverything()
            {
                return Func(_ => _.ComputeAnswerToLifeAndUniverseEverything());
            }

            public void AddToList(IList<string> strings)
            {
                Action(_ => _.AddToList(strings));
            }

            public int Id
            {
                get { return Func(_ => _.Id); }
            }
        }

        private Mock<IThing> _mockThing;

        private RestartableThing CreateRestartableThing()
        {
            var result = new RestartableThing(() => _mockThing.Object);
            return result;
        }

        [TestInitialize]
        public void ConfigureMock()
        {
            _mockThing = new Mock<IThing>(MockBehavior.Strict);
        }

        [TestMethod]
        public void NoAdverseEventsCallingProperty()
        {
            _mockThing.SetupGet(_ => _.Id).Returns(11);
            var cut = CreateRestartableThing();

            var actual = cut.Id;

            Assert.AreEqual(11, actual);
            _mockThing.VerifyGet(_ => _.Id, Times.Once);
        }

        [TestMethod]
        public void NoAdverseEventsCallingMethod()
        {
            _mockThing.Setup(_ => _.ComputeAnswerToLifeAndUniverseEverything()).Returns(42);
            var cut = CreateRestartableThing();

            var actual = cut.ComputeAnswerToLifeAndUniverseEverything();

            Assert.AreEqual(42, actual);
            _mockThing.Verify(_ => _.ComputeAnswerToLifeAndUniverseEverything(), Times.Once);
        }

        [TestMethod]
        public void AlsoSupportsMethodWithoutReturn()
        {
            Expression<Action<IThing>> expression = _ => _.AddToList(It.IsAny<IList<string>>());
            _mockThing.Setup(expression).Callback<IList<string>>(_ => _.Add("foo"));
            var cut = CreateRestartableThing();
            var list = new List<string>();

            cut.AddToList(list);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("foo", list[0]);
            _mockThing.Verify(expression, Times.Once);
        }

        [TestMethod]
        public void SingleExceptionCausesSingleRetry()
        {
            var callCount = 0;
            _mockThing.SetupGet(_ => _.Id).Callback(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw new SecurityException();
                }
            }
                ).Returns(11);
            var cut = CreateRestartableThing();

            var actual = cut.Id;

            Assert.AreEqual(11, actual);
            _mockThing.VerifyGet(_ => _.Id, Times.Exactly(2));
        }

        [TestMethod]
        public void AllExceptionCausesGiveUp()
        {
            Expression<Func<IThing, int>> expression =
                _ => _.ComputeAnswerToLifeAndUniverseEverything();
            _mockThing.Setup(expression).Throws<SecurityException>();
            var cut = CreateRestartableThing();

            var caughtException = false;
            try
            {
                cut.ComputeAnswerToLifeAndUniverseEverything();
            }
            catch (InvalidOperationException ioe)
            {
                caughtException = true;
                Assert.IsInstanceOfType(ioe.InnerException, typeof(SecurityException));
            }

            Assert.AreEqual(true, caughtException);
            _mockThing.Verify(expression, Times.Exactly(RestartableBase<IThing, SecurityException>.MaximumAttempts));
        }
    }
}
