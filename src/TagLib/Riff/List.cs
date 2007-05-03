/***************************************************************************
    copyright            : (C) 2007 by Brian Nickel
    email                : brian.nickel@gmail.com
 ***************************************************************************/

/***************************************************************************
 *   This library is free software; you can redistribute it and/or modify  *
 *   it  under the terms of the GNU Lesser General Public License version  *
 *   2.1 as published by the Free Software Foundation.                     *
 *                                                                         *
 *   This library is distributed in the hope that it will be useful, but   *
 *   WITHOUT ANY WARRANTY; without even the implied warranty of            *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU     *
 *   Lesser General Public License for more details.                       *
 *                                                                         *
 *   You should have received a copy of the GNU Lesser General Public      *
 *   License along with this library; if not, write to the Free Software   *
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  *
 *   USA                                                                   *
 ***************************************************************************/

using System;
using System.Collections.Generic;
namespace TagLib.Riff
{
   public class List : Dictionary <ByteVector,ByteVectorList>
   {
      public List () : base ()
      {}
      
      public List (ByteVector data) : this ()
      {
         Parse (data);
      }
      
      public List (TagLib.File file, long position, int length) : this ()
      {
         file.Seek (position);
         Parse (file.ReadBlock (length));
      }
      
      private void Parse (ByteVector data)
      {
         int offset = 0;
         while (offset + 8 < data.Count)
         {
            ByteVector id = data.Mid (offset, 4);
            int length = (int) data.Mid (offset + 4, 4).ToUInt (false);
            
            if (!ContainsKey (id))
               Add (id, new ByteVectorList ());
            
            this [id].Add (data.Mid (offset + 8, length));
            
            offset += 8 + length;
            
            if (length % 2 == 1)
               length ++;
         }
      }
      
      public ByteVector Render ()
      {
         ByteVector data = new ByteVector ();
         
         foreach (ByteVector id in Keys)
            foreach (ByteVector value in this [id])
            {
               if (value.Count == 0)
                  continue;
            
               data.Add (id);
               data.Add (ByteVector.FromUInt ((uint) value.Count, false));
               data.Add (value);
               
               if (data.Count % 2 == 1)
                  data.Add (0);
           }
         
         return data;
      }
      
      public ByteVector RenderEnclosed (ByteVector id)
      {
         ByteVector data = Render ();
         
         if (data.Count <= 8)
            return new ByteVector ();
         
         ByteVector header = new ByteVector ("LIST");
         header.Add (ByteVector.FromUInt ((uint) (data.Count + 4), false));
         header.Add (id);
         data.Insert (0, header);
         return data;
      }
      
      public ByteVectorList GetValues (ByteVector id)
      {
         ByteVectorList value;
         return TryGetValue (id, out value) ? value : new ByteVectorList ();
      }
      
      public StringList GetValuesAsStringList (ByteVector id)
      {
         StringList list = new StringList ();
         
         foreach (ByteVector data in GetValues (id))
         {
            if (data == null)
               continue;
         
            int str_length = data.Count;
            while (str_length > 0 && data [str_length - 1] == 0)
               str_length --;
            
            list.Add (data.Mid (0, str_length).ToString (StringType.UTF8));
         }
         
         return list;
      }
      
      public uint GetValueAsUInt (ByteVector id)
      {
         foreach (string str in GetValuesAsStringList (id))
         {
            uint value;
            if (str != null && uint.TryParse (str, out value))
               return value;
         }
         
         return 0;
      }
      
      public void SetValue (ByteVector id, IEnumerable<ByteVector> values)
      {
         if (values == null)
            RemoveValue (id);
         else if (ContainsKey (id))
            this [id] = new ByteVectorList (values);
         else
            Add (id, new ByteVectorList (values));
      }
      
      public void SetValue (ByteVector id, params ByteVector [] values)
      {
         SetValue (id, values as IEnumerable<ByteVector>);
      }
      
      public void SetValue (ByteVector id, uint value)
      {
         if (value == 0)
            RemoveValue (id);
         else
            SetValue (id, value.ToString ());
      }
      
      public void SetValue (ByteVector id, IEnumerable<string> values)
      {
         if (values == null)
         {
            RemoveValue (id);
            return;
         }
         
         ByteVectorList l = new ByteVectorList ();
         foreach (string value in values)
         {
            if (string.IsNullOrEmpty (value))
               continue;
            
            ByteVector data = ByteVector.FromString (value, StringType.UTF8);
            data.Add (0);
            l.Add (data);
         }
         
         SetValue (id, l);
      }
      
      public void SetValue (ByteVector id, params string [] values)
      {
         SetValue (id, values as IEnumerable<string>);
      }
      
      public void RemoveValue (ByteVector id)
      {
         if (ContainsKey (id))
            Remove (id);
      }
   }
}