using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetDrawCard : NetMessage
{
    public int team;   // 0=white, 1=black
    public int cardId; // CardData.id¡]Ã­©wID¡^

    public NetDrawCard()
    {
        Code = OpCode.DRAW_CARD;
    }

    public NetDrawCard(DataStreamReader reader)
    {
        Code = OpCode.DRAW_CARD;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(team);
        writer.WriteInt(cardId);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        team = reader.ReadInt();
        cardId = reader.ReadInt();
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_DRAW_CARD?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_DRAW_CARD?.Invoke(this, cnn);
    }
}
