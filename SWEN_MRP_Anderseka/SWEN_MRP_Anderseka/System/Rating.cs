using System.Collections.Generic;

namespace MyMediaList.System
{
    public sealed class Rating : Atom, IAtom
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // static in-memory store                                                                                    //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Dictionary<int, Rating> _Ratings = new();
        private static int _NextId = 1;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private fields                                                                                             //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool _New;
        private int _Id;
        private string? _UserName = null;
        private int _MediaId;
        private int _Value;
        private string? _Comment = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructor                                                                                                //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Rating(Session? session = null)
        {
            _EditingSession = session;
            _New = true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // static helper                                                                                              //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static Rating? Get(int id)
        {
            lock (_Ratings)
            {
                if (_Ratings.ContainsKey(id))
                {
                    return _Ratings[id];
                }
            }
            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // properties                                                                                                 //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Id => _Id;

        public string UserName
        {
            get => _UserName ?? string.Empty;
            set => _UserName = value;
        }

        public int MediaId
        {
            get => _MediaId;
            set => _MediaId = value;
        }

        public int Value
        {
            get => _Value;
            set => _Value = value;
        }

        public string Comment
        {
            get => _Comment ?? string.Empty;
            set => _Comment = value;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Atom overrides (Save, Delete, Refresh)                                                                     //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public override void Save()
        {
            lock (_Ratings)
            {
                if (_New)
                {
                    _Id = _NextId++;
                    _Ratings[_Id] = this;
                    _New = false;
                }
                else
                {
                    _Ratings[_Id] = this;
                }
            }

            _EndEdit();
        }

        public override void Delete()
        {
            lock (_Ratings)
            {
                if (_Ratings.ContainsKey(_Id))
                {
                    _Ratings.Remove(_Id);
                }
            }

            _EndEdit();
        }

        public override void Refresh()
        {
            lock (_Ratings)
            {
                if (_Ratings.ContainsKey(_Id))
                {
                    var r = _Ratings[_Id];
                    _UserName = r._UserName;
                    _MediaId = r._MediaId;
                    _Value = r._Value;
                    _Comment = r._Comment;
                }
            }

            _EndEdit();
        }
    }
}