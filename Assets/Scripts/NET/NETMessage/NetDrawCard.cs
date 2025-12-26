using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetDrawCard : NetMessage
{
    public int deckIndex; // 抽到的牌在 deckData 裡的索引 (第幾張)

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
        writer.WriteInt(deckIndex); // 寫入抽到的卡牌編號
    }

    public override void Deserialize(DataStreamReader reader)
    {
        deckIndex = reader.ReadInt();
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