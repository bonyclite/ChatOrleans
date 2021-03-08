using System;

namespace Core.Models.Chat
{
    public class CreateChatModel
    {
        public string Name { get; set; }
        public bool IsPrivate { get; set; }

        public CreateChatModel()
        {
            object a = 1;

           var xyek = new Xyek();

           if (xyek)
           {
               
           }
        }
    }

    public interface IBase
    {

    }
    
    public class Base : IBase
    {
        public static implicit operator bool(Base x)
        {
            return x != null;
        }   
    }

    public class Xyek : Base
    {
        
    }
}