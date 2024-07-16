using Elasticsearch.DataMatching;
using MusicManager.Core.ViewModules;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.Infrastructure.Extentions
{
    public static class MLTrackEditExtention
    {      

        

        

        public static string CheckByValueType(this List<DHValueType> dHValueTypes, string type)
        {
            if (dHValueTypes != null && dHValueTypes.Count > 0)
            {
                foreach (var item in dHValueTypes)
                {
                    if (type == item.type)
                    {
                        return item.value;
                    }
                }
            }
            return "";
        }

        private static string GetIPName(this List<DHTInterestedParty> dHTInterestedParties, string role)
        {
            if (dHTInterestedParties != null && dHTInterestedParties.Count > 0)
            {
                foreach (var item in dHTInterestedParties)
                {
                    if (role == item.role)
                    {
                        return item.name;
                    }
                }
            }
            return "";
        }

        private static List<string> GetNameListByRole(string role, ICollection<DHTInterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.role == role).Select(a => a.name).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        private static List<DHTInterestedParty> MapIpsFromEditTrack(this List<DHTInterestedParty> dHTInterestedParties, string role, List<string> editList)
        {
            if (editList == null)
                return dHTInterestedParties;

            List<DHTInterestedParty> _ipList = new List<DHTInterestedParty>();

            if (dHTInterestedParties != null)
                _ipList = dHTInterestedParties;
          
            _ipList.RemoveAll(a => a.role == role);

            List<DHTInterestedParty> _dHTInterestedParties = editList.Select(a => new DHTInterestedParty()
            {
                role = role,
                name = a
            }).ToList();
            _ipList.AddRange(_dHTInterestedParties);
            return _ipList;
        }



    }
}
