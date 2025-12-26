using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetUseCard : NetMessage
{
    public int handIndex; 

    public NetUseCard()
    {
        Code = OpCode.USE_CARD;
    }

    public NetUseCard(DataStreamReader reader)
    {
        Code = OpCode.USE_CARD;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(handIndex);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        handIndex = reader.ReadInt();
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_USE_CARD?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_USE_CARD?.Invoke(this, cnn);
    }
}