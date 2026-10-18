using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegend.Arg
{
    [Serializable] public class ChatEntry
    {
        public string sender, text, image;
        public ChatEntry(string sender, string text, string image = "")
        { this.sender = sender; this.text = text; this.image = image; }
    }
    [Serializable] public class SaveData
    {
        public int version = 1;
        public string page = "home", query = "";
        public bool loggedIn, arrival, recordRead;
        public int conversation;
        public List<string> history = new List<string>();
        public List<ChatEntry> chat = new List<ChatEntry>();
    }

    // All story transitions are explicit and synchronous. Visual delay is optional;
    // there is no timer that can harm the friend or lose a pending message.
    public sealed class ArgSession
    {
        public SaveData State { get; private set; }
        public ArgSession(SaveData state = null)
        {
            State = state != null && state.version == 1 ? state : new SaveData();
            State.history = State.history ?? new List<string>();
            State.chat = State.chat ?? new List<ChatEntry>();
            if (State.chat.Count == 0)
            {
                Friend("方便开电脑吗？我到旧站资料室了。管理员把钥匙和移交表留好了，对完箱子我就走。");
                Friend("有一箱是‘回声17’整理的。搜‘沪上异闻录 回声’，商场和地铁那几篇都是她收的。");
                Friend("旧索引地址：huisheng.invalid。搬迁阅览口令是 huisheng09。你找找她以前的用户 ID，帮我开一下目录。");
            }
        }
        void Friend(string text, string image = "") => State.chat.Add(new ChatEntry("周祁", text, image));
        void Player(string text) => State.chat.Add(new ChatEntry("我", text));
        public bool Login(string id, string password)
        {
            if (!string.Equals(id?.Trim(), "hx_0417", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(password?.Trim(), "huisheng09", StringComparison.OrdinalIgnoreCase)) return false;
            State.loggedIn = true;
            Arrive();
            return true;
        }
        void Arrive()
        {
            if (State.arrival) return;
            State.arrival = true;
            Friend("我先把登记填了，今天就我一个人来。", "IMG02");
            Friend("最上面这箱标签还在，你那边能按这个查吗？");
        }
        public bool Visit(WebPage page)
        {
            if (page == null || (page.restricted && !State.loggedIn)) return false;
            State.page = page.id;
            if (!State.history.Contains(page.id)) State.history.Add(page.id);
            if (page.id == "record") State.recordRead = true;
            return true;
        }
        public string[] Replies()
        {
            if (State.conversation == 0 && State.arrival && State.recordRead)
                return new[] {"先出来，这箱以前出过问题。", "你看一下刚才的登记表。"};
            if (State.conversation == 1) return new[] {"没有，我一直在打字。"};
            if (State.conversation == 2) return new[] {"沈叙？", "别等了，先出来。", "接电话。"};
            if (State.conversation == 3) return Array.Empty<string>();
            return new[] {"为什么不用昵称？", "ID 从哪里找？", "再给我一点提示。"};
        }
        public bool Reply(string text)
        {
            if (Array.IndexOf(Replies(), text) < 0) return false;
            Player(text);
            if (State.conversation == 0 && State.arrival && State.recordRead)
            {
                Friend(text.StartsWith("先出来") ? "好，我把箱盖放回去就走。" : "就在门口桌上，我去看。");
                Friend("刚才是不是你发语音了？");
                State.conversation = 1;
            }
            else if (State.conversation == 1)
            {
                Friend("我听见有人叫我名字，还以为是你。等下。");
                Friend("", "IMG03");
                Friend("我没填这个。");
                State.conversation = 2;
            }
            else if (State.conversation == 2)
            {
                if (text == "沈叙？") Player("我以前有个同学叫这个名字。");
                State.conversation = 3;
            }
            else if (text == "为什么不用昵称？") Friend("我试过回声17，不认。登录页说要以前的资料员编号。");
            else if (text == "ID 从哪里找？") Friend("看看她的旧帖。她说过编号在一篇采访的配图里。");
            else Friend("采访叫《把怪谈留在纸上》。打开报道的原图，找那张资料员卡。");
            return true;
        }
        public string Serialize() => JsonUtility.ToJson(State, true);
    }
}
