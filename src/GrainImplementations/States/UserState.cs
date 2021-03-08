using System;
using System.Collections.Generic;

namespace GrainImplementations.States
{
    public class UserState
    {
        public Guid UserId { get; set; }
        public List<Guid> JoinedChats { get; set; }
        public Guid? ConnectedChatId { get; set; }

        public UserState()
        {
            JoinedChats = new List<Guid>();
        }
    }
}