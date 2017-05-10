namespace JoyOI.UserCenter.SDK
{
    public class ResponseBody<T>
    {
        public int code { get; set; }

        public string msg { get; set; }

        public T data { get; set; }
    }

    public class ResponseBody : ResponseBody<dynamic>
    { }
}
