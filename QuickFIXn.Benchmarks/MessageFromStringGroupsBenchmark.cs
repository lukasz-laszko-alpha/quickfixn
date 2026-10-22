using System;
using System.Globalization;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using QuickFix;
using QuickFix.Fields;
using DD = QuickFix.DataDictionary.DataDictionary;

namespace QuickFix.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

[MemoryDiagnoser]
public class MessageFromStringGroupsBenchmark
{
    private readonly Message _message = new();
    private DD _dataDictionary = null!;
    private string _wireMessage = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        using Stream dictionary = typeof(MessageFromStringGroupsBenchmark).Assembly
            .GetManifestResourceStream("QuickFIXn.Benchmarks.FIX44.xml")
            ?? throw new InvalidOperationException("FIX44 dictionary resource is missing.");
        _dataDictionary = new DD(dictionary);
        _wireMessage = CreateWireMessage();

        _message.FromString(_wireMessage, true, _dataDictionary, _dataDictionary);
        VerifyParsedGroups();
    }

    [Benchmark]
    public void ParseExecutionReportWithGroups()
    {
        _message.FromString(_wireMessage, true, _dataDictionary, _dataDictionary);
    }

    private static string CreateWireMessage()
    {
        var message = new Message();
        message.Header.SetField(new StringField(Tags.BeginString, "FIX.4.4"));
        message.Header.SetField(new StringField(Tags.MsgType, "8"));
        message.Header.SetField(new StringField(Tags.MsgSeqNum, "360"));
        message.Header.SetField(new StringField(Tags.SenderCompID, "SENDER"));
        message.Header.SetField(new StringField(Tags.SendingTime, "20130321-15:21:23"));
        message.Header.SetField(new StringField(Tags.TargetCompID, "TARGET"));

        message.SetField(new StringField(Tags.OrderID, "ORDER-1"));
        message.SetField(new StringField(Tags.ExecID, "EXEC-1"));
        message.SetField(new StringField(Tags.ExecType, "F"));
        message.SetField(new StringField(Tags.OrdStatus, "2"));
        message.SetField(new StringField(Tags.Symbol, "SYM"));
        message.SetField(new StringField(Tags.Side, "1"));
        message.SetField(new StringField(Tags.LeavesQty, "0"));
        message.SetField(new StringField(Tags.CumQty, "1"));
        message.SetField(new StringField(Tags.AvgPx, "42.5"));

        for (int index = 1; index <= 6; ++index)
        {
            var party = new Group(Tags.NoPartyIDs, Tags.PartyID);
            party.SetField(new StringField(Tags.PartyID, $"PARTY-{index}"));
            party.SetField(new StringField(Tags.PartyIDSource, "D"));
            party.SetField(new StringField(Tags.PartyRole, index.ToString(CultureInfo.InvariantCulture)));

            if (index == 1)
            {
                var subParty = new Group(Tags.NoPartySubIDs, Tags.PartySubID);
                subParty.SetField(new StringField(Tags.PartySubID, "SUB-1"));
                subParty.SetField(new StringField(Tags.PartySubIDType, "4"));
                party.AddGroup(subParty);
            }

            message.AddGroup(party);
        }

        return message.ConstructString();
    }

    private void VerifyParsedGroups()
    {
        if (_message.GetInt(Tags.NoPartyIDs) != 6
            || _message.GetGroup(1, Tags.NoPartyIDs).GetString(Tags.PartyID) != "PARTY-1"
            || _message.GetGroup(6, Tags.NoPartyIDs).GetString(Tags.PartyID) != "PARTY-6"
            || _message.GetGroup(1, Tags.NoPartyIDs).GetGroup(1, Tags.NoPartySubIDs).GetString(Tags.PartySubID) != "SUB-1")
        {
            throw new InvalidOperationException("The FIX44 repeating-group message did not parse as expected.");
        }
    }
}
