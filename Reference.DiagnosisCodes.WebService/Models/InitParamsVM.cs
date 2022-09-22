namespace Reference.DiagnosisCodes.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public struct InitParamsVM
    {
        public string Suffix   { get; set; }
        public int?   MaxCount { get; set; }

#if DEBUG
        public override string ToString() => Suffix;
#endif
    }
}
