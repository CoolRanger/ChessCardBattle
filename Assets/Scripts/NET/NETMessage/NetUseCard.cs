using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetUseCard : NetMessage
{
    public int handIndex;
    public int cardId;

    public int targetX;
    public int targetY;

    public int stepCost;

    public NetUseCard()
    {
        Code = OpCode.USE_CARD;

        targetX = -1;
        targetY = -1;
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
        writer.WriteInt(cardId);

        writer.WriteInt(targetX);
        writer.WriteInt(targetY);
        writer.WriteInt(stepCost);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        handIndex = reader.ReadInt();
        cardId = reader.ReadInt();

        targetX = reader.ReadInt();
        targetY = reader.ReadInt();
        stepCost  = reader.ReadInt();
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