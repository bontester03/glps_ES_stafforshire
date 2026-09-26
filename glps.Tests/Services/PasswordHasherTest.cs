using glps.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace glps.Tests.Services
{
    [TestClass]
    public class PasswordHasherTest
    {
        [TestMethod]
        public void HashThenVerify()
        {
            var hash = PasswordHasher.Hash("s3cret!");

            Assert.IsTrue(PasswordHasher.IsHashed(hash));
            Assert.AreNotEqual("s3cret!", hash);
            Assert.IsTrue(PasswordHasher.Verify("s3cret!", hash));
            Assert.IsFalse(PasswordHasher.Verify("wrong", hash));
        }

        [TestMethod]
        public void HashesAreSalted()
        {
            Assert.AreNotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));
        }

        [TestMethod]
        public void LegacyPlaintextStillVerifies()
        {
            Assert.IsFalse(PasswordHasher.IsHashed("plain"));
            Assert.IsTrue(PasswordHasher.Verify("plain", "plain"));
            Assert.IsFalse(PasswordHasher.Verify("Plain", "plain"));
        }

        [TestMethod]
        public void MalformedHashFails()
        {
            Assert.IsFalse(PasswordHasher.Verify("x", "PBKDF2$abc$$"));
            Assert.IsFalse(PasswordHasher.Verify("x", null));
        }
    }
}
