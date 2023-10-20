using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using Paprika.Chain;
using Paprika.Crypto;
using Paprika.Data;
using Paprika.Merkle;

namespace Paprika.Tests.Merkle;

/// <summary>
/// The tests are based on Nethermind's suite provided at
/// <see cref="https://github.com/NethermindEth/nethermind/blob/feature/paprika_merkle_tests/src/Nethermind/Nethermind.Trie.Test/PaprikaTrieTests.cs"/>
/// </summary>
public class RootHashTests
{
    [Test]
    public void Empty_tree()
    {
        var commit = new Commit();

        AssertRoot("56E81F171BCC55A6FF8345E692C0F86E5B48E01B996CADC001622FB5E363B421", commit);
    }

    [Test]
    public void Single_account()
    {
        var commit = new Commit();

        var key = Values.Key0;
        var account = new Account(Values.Balance0, Values.Nonce0);

        commit.Set(Key.Account(key), account.WriteTo(stackalloc byte[Account.MaxByteCount]));

        AssertRoot("E2533A0A0C4F1DDB72FEB7BFAAD12A83853447DEAAB6F28FA5C443DD2D37C3FB", commit);
    }

    [Test]
    public void Branch_two_leafs()
    {
        var commit = new Commit();

        const byte nibbleA = 0x10;
        var balanceA = Values.Balance0;
        var nonceA = Values.Nonce0;

        const byte nibbleB = 0x20;
        var balanceB = Values.Balance1;
        var nonceB = Values.Nonce1;

        Span<byte> span = stackalloc byte[32];
        span.Fill(0);

        span[0] = nibbleA;
        commit.Set(Key.Account(new Keccak(span)),
            new Account(balanceA, nonceA).WriteTo(stackalloc byte[Account.MaxByteCount]));

        span[0] = nibbleB;
        commit.Set(Key.Account(new Keccak(span)),
            new Account(balanceB, nonceB).WriteTo(stackalloc byte[Account.MaxByteCount]));

        AssertRoot("73130daa1ae507554a72811c06e28d4fee671bfe2e1d0cef828a7fade54384f9", commit);
    }

    [Test]
    public void Extension()
    {
        var commit = new Commit();

        var balanceA = Values.Balance0;
        var nonceA = Values.Nonce0;

        var balanceB = Values.Balance1;
        var nonceB = Values.Nonce1;

        commit.Set(Key.Account(Values.Key0),
            new Account(balanceA, nonceA).WriteTo(stackalloc byte[Account.MaxByteCount]));
        commit.Set(Key.Account(Values.Key1),
            new Account(balanceB, nonceB).WriteTo(stackalloc byte[Account.MaxByteCount]));

        AssertRoot("a624947d9693a5cba0701897b3a48cb9954c2f4fd54de36151800eb2c7f6bf50", commit);
    }

    [TestCase(0, "7f7fd47a28dc4dbfd1b1b33d254da8be74deab55bef81a02c232ca9957e05689", TestName = "From the Root")]
    [TestCase(Keccak.Size - 2, "d8fc42b5f9491f526d0935445e9b83d8ddde46978cc450a6d1f83351da1bfae2", TestName = "At the bottom")]
    [TestCase(Keccak.Size - 16, "6cb4831677e5dc9f8b6aaa9554c8be152ead051c35bdadbc19ae7a0242904836", TestName = "Split in the middle")]
    public void Skewed_tree(int startFromByte, string rootHash)
    {
        var commit = new Commit();
        Random random = new(17);

        // "0000"
        // "1000"
        // "1100";
        // "1110";
        // "1111";
        // "2000"
        // "2100"
        // "2110"
        // "2111"
        // "2200"
        // ...

        Span<byte> destination = stackalloc byte[Account.MaxByteCount];
        Span<byte> key = stackalloc byte[32];
        for (var nibble0 = 0; nibble0 < 16; nibble0++)
        {
            for (var nibble1 = 0; nibble1 <= nibble0; nibble1++)
            {
                for (var nibble2 = 0; nibble2 <= nibble1; nibble2++)
                {
                    for (var nibble3 = 0; nibble3 <= nibble2; nibble3++)
                    {
                        key.Clear();
                        random.NextBytes(key.Slice(startFromByte));
                        var b0 = (byte)((nibble0 << 4) | nibble1);
                        key[startFromByte] = b0;

                        var b1 = (byte)((nibble2 << 4) | nibble3);
                        key[startFromByte + 1] = b1;

                        var account = new Account(b1, b0, new Keccak(key), Keccak.EmptyTreeHash);
                        commit.Set(Key.Account(NibblePath.FromKey(key)), account.WriteTo(destination));
                    }
                }
            }
        }

        AssertRoot(rootHash, commit);
    }

    [TestCase(Keccak.Size - 1, "456af6194513cb6b8fd475087fdac6ab60bb3b3f154d06e19e3b18cd8c7e7092",
        TestName = "At the bottom")]
    public void Skewed_tree_short(int startFromByte, string rootHash)
    {
        var commit = new Commit();
        Random random = new(17);

        const int maxNibble = 3;
        // "00"
        // "10"
        // "11"
        // "20"
        // "21"
        // "22"

        Span<byte> destination = stackalloc byte[Account.MaxByteCount];
        Span<byte> key = stackalloc byte[32];
        for (var nibble0 = 0; nibble0 < maxNibble; nibble0++)
        {
            for (var nibble1 = 0; nibble1 <= nibble0; nibble1++)
            {
                key.Clear();
                random.NextBytes(key.Slice(startFromByte));
                var b0 = (byte)((nibble0 << 4) | nibble1);
                key[startFromByte] = b0;

                Console.WriteLine($"Case: {b0}");

                var account = new Account((uint)b0 + 1, b0, new Keccak(key), Keccak.EmptyTreeHash);
                commit.Set(Key.Account(NibblePath.FromKey(key)), account.WriteTo(destination));
            }
        }

        AssertRoot(rootHash, commit);
    }
    
    [TestCase(16, "80ad0d5d5f4912fe515d70f62b0c5359ac8fb26dbaf52a78fafb7a820252438c")]
    [TestCase(128, "45f09d49b6b1ca5f2b0cb82bc7fb3b381ff2ce95bd69a5eda8ce13b48855c2e4")]
    [TestCase(512, "b71064764d77f2122778e3892b37470854efd4a4acd8a1955bbe7dcfa0bc161c")]
    public void Scattered_tree(int size, string rootHash)
    {
        Random random = new(17);
        Span<byte> key = stackalloc byte[32];
        Span<byte> destination = stackalloc byte[Account.MaxByteCount];

        var commit = new Commit();

        for (uint i = 0; i < size; i++)
        {
            key.Clear();
            
            var at = random.Next(NibblePath.KeccakNibbleCount);
            var nibble = random.Next(16);

            if (at % 2 == 0)
            {
                nibble <<= 4;
            }

            key[at / 2] = (byte)nibble;

            var account = new Account(i, i, new Keccak(key), Keccak.EmptyTreeHash);
            commit.Set(Key.Account(NibblePath.FromKey(key)), account.WriteTo(destination));
        }
        
        AssertRoot(rootHash, commit);
    }

    private static void AssertRoot(string hex, ICommit commit)
    {
        var merkle = new ComputeMerkleBehavior();

        merkle.BeforeCommit(commit);

        var keccak = new Keccak(Convert.FromHexString(hex));

        merkle.RootHash.Should().Be(keccak);
    }
}