namespace ModulesFramework
{
    public class EcsOneData<T> : OneData where T : struct
    {
        private T _data;
        private bool _isSet;

        public EcsOneData(bool isCreateDefault = false)
        {
            _isSet = isCreateDefault;
        }

        public void SetData(in T data)
        {
            _data = data;
            _isSet = true;
        }

        public void SetDataIfNotExist(in T data)
        {
            if (_isSet)
                return;
            SetData(data);
        }

        public ref T GetData()
        {
            return ref _data;
        }

        public bool IsExist()
        {
            return _isSet;
        }

        internal override object GetDataObject()
        {
            return _data;
        }
    }

    public abstract class OneData
    {
        internal abstract object GetDataObject();
    }
}