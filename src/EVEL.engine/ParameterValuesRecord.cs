using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using System.IO;

namespace Evel.engine.parametersImport {
    public class ParameterValuesRecord {

        private byte[][] data = null;
        //private byte[] readBuffer = new byte[sizeof(float)];

        public ParameterValuesRecord(List<ISpectrum> spectra) {
            SetData(spectra);
        }

        public ParameterValuesRecord(byte[][] data) {
            this.data = data;
        }

        public void SetData(List<ISpectrum> spectra) {
            if (this.data == null)
                this.data = new byte[spectra.Count][];
            else if (this.data.Length < spectra.Count)
                this.data = new byte[spectra.Count][];
            for (int i = 0; i < spectra.Count; i++)
                ExtractData(spectra[i].Parameters, spectra[i].Name, (float)spectra[i].Fit, ref data[i]);
        }

        //public ParameterValuesRecord(List<IParameterSet> parameters

        #region Data IO

        /// <summary>
        /// | byte | spectrum name (length) | float || byte  | float         | byte   | byte  | byte  | byte || float | byte   | byte  |   |
        /// |------|------------------------|-------||-------|---------------|--------|-------|-------|------||-------|--------|-------|...|
        /// | name |                        | chisq || group | contribution  | status | ref   | comp  | comp || value | status | ref   |   |
        /// | size |                        |       ||  ID   | or -1.0f      |        | group | count | size ||       |        | group |   |
        ///                                          |                                                        |------------------------|   |
        ///                                          |                                                                     *               |
        ///                                          |-------------------------------------------------------------------------------------|
        ///                                                                                      *                  
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        private void ExtractData(IParameterSet parameters, string name, float fit, ref byte[] sdata) {
            //             data size      name + \0                  chisq
            uint datasize = (uint)(sizeof(uint) + name.Length + 1 + sizeof(float));
            byte g, c, p;
            for (g = 0; g < parameters.GroupCount; g++)
                if (parameters[g] != null)
                if ((parameters[g].Definition.Type & GroupType.Hidden) == 0) {
                    datasize += sizeof(byte) + sizeof(float) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(byte);
                    //if (parameters[g].Components.Size > 0)
                        for (c=0; c<parameters[g].Components.Size; c++)
                            for (p=0; p<parameters[g].Components[c].Size; p++)
                                if ((parameters[g].Components[c][p].Definition.Properties & (ParameterProperties.Readonly | ParameterProperties.Hidden)) == 0)
                                    datasize += sizeof(float) + sizeof(byte) + sizeof(byte);
                }
            //byte[] sdata = new byte[datasize];
            if (sdata == null)
                sdata = new byte[datasize];
            else if (sdata.Length < datasize)
                sdata = new byte[datasize];

            MemoryStream stream = null;
            
            try {
                stream = new MemoryStream(sdata);
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    //name size
                    writer.Write(datasize);
                    //name
                    if (name.Length > byte.MaxValue)
                        writer.Write(name.Substring(0, byte.MaxValue));
                    else
                        writer.Write(name);
                    //chisq
                    writer.Write((float)fit);
                    //groups
                    for (g = 0; g < parameters.GroupCount; g++)
                        if (parameters[g] != null)
                            if ((parameters[g].Definition.Type & GroupType.Hidden) == 0) {
                                //datasize += sizeof(byte) + sizeof(float) + sizeof(byte) + sizeof(byte);
                                writer.Write(g);
                                if (parameters[g] is ContributedGroup)
                                    WriteParameter(writer, ((ContributedGroup)parameters[g]).contribution);
                                else
                                    WriteParameter(writer, null);
                                
                                writer.Write((byte)parameters[g].Components.Size);
                                if (parameters[g].Components.Size > 0) {
                                    writer.Write((byte)parameters[g].Components[0].Size);
                                    for (c = 0; c < parameters[g].Components.Size; c++)
                                        for (p = 0; p < parameters[g].Components[c].Size; p++)
                                            WriteParameter(writer, parameters[g].Components[c][p]);
                                } else
                                    writer.Write((byte)0);
                            }
                }
            } finally {
                if (stream != null)
                    stream.Close();
            }
            //return sdata;
        }

        internal void FillSpectrum(ISpectrum spectrum, int dataId, ParameterStatus status) {
            FillSpectrum(this.data[dataId], spectrum, status);
        }

        public void FillSpectrum(ISpectrum spectrum, ParameterStatus status) {
            this.FillSpectrum(spectrum, FindSpectrum(spectrum.Name), status);
        }

        private int FindSpectrum(string spectrumName) {
            int result = -1;
            for (int i = 0; i < data.GetLength(0) && result == -1; i++) {
                try {
                    MemoryStream ms = new MemoryStream(data[i]);
                    try {
                        using (BinaryReader reader = new BinaryReader(ms)) {
                            reader.ReadUInt32();
                            if (spectrumName == reader.ReadString())
                                result = i;

                        }
                    } finally {
                        ms.Close();
                    }
                } catch (Exception) { }
            }
            if (result == -1)
                result = 0;
            return result;
        }

        public void RestoreParameter(ISpectrum spectrum, ParameterLocation location) {
            this.RestoreParameter(data[FindSpectrum(spectrum.Name)], spectrum, location);
        }

        private void RestoreParameter(byte[] sdata, ISpectrum spectrum, ParameterLocation loc) {
            uint size;
            using (MemoryStream stream = new MemoryStream(sdata)) {
                BinaryReader reader = new BinaryReader(stream);
                try {
                    size = reader.ReadUInt32();
                    reader.ReadString();
                    reader.ReadSingle(); //chisq
                    byte g, c, p, cc, cs;
                    while (stream.Position < size) {
                        g = reader.ReadByte(); // sdata[position++];
                        if (spectrum.Parameters[g] is ContributedGroup && loc.compId == -1 && loc.parId == -1 && loc.groupId == g) {
                            ReadParameter(reader, ((ContributedGroup)spectrum.Parameters[g]).contribution, 0);
                            break;
                        } else
                            ReadParameter(reader, null, 0);
                        cc = reader.ReadByte();
                        cs = reader.ReadByte();

                        if (spectrum.Parameters[g].Components.Size > 0) {

                            for (c = 0; c < cc && size > 0u; c++)
                                for (p = 0; p < cs; p++)
                                    if (loc.compId == c && loc.parId == p && loc.groupId == g) {
                                        ReadParameter(reader, spectrum.Parameters[g].Components[c][p], 0);
                                        size = 0u;
                                        break;
                                    } else
                                        ReadParameter(reader, spectrum.Parameters[g].Components[c][p], ~ParameterStatus.None);
                        } else {
                            reader.ReadBytes(6 * cc * cs);
                            //position += (sizeof(float) + sizeof(byte) + sizeof(byte)) * cc * cs;
                        }
                    }
                } catch (EndOfStreamException) {
                } finally {
                    reader.Close();
                }
            }
        }

        private void FillSpectrum(byte[] sdata, ISpectrum spectrum, ParameterStatus status) {
            //read group
            uint size;
            using (MemoryStream stream = new MemoryStream(sdata)) {
                BinaryReader reader = new BinaryReader(stream);
                try {
                    size = reader.ReadUInt32();
                    reader.ReadString();
                    //int position = sizeof(byte) + BitConverter.ToInt16(sdata, 0);
                    float fit = reader.ReadSingle();
                    if (status > 0)
                        spectrum.Fit = fit; // ReadSingle(sdata, ref position);
                    byte g, c, p, cc, cs;
                    while (stream.Position < size) {
                        g = reader.ReadByte(); // sdata[position++];
                        if (spectrum.Parameters[g] is ContributedGroup)
                            ReadParameter(reader, ((ContributedGroup)spectrum.Parameters[g]).contribution, status);
                        else
                            ReadParameter(reader, null, 0);
                        cc = reader.ReadByte();
                        cs = reader.ReadByte();

                        //resize spectrum if neccessary
                        if (spectrum.Parameters[g].Components.Size != cc && 
                            spectrum.Parameters[g].Definition.componentCount == 0 || 
                            spectrum.Parameters[g].Components.Size != spectrum.Parameters[g].Definition.componentCount)
                            spectrum.Parameters[g].Components.Size = cc;

                        if (spectrum.Parameters[g].Components.Size > 0) {

                            for (c = 0; c < cc; c++)
                                for (p = 0; p < cs; p++)
                                    if (c < spectrum.Parameters[g].Components.Size && p < spectrum.Parameters[g].Components[0].Size)
                                        ReadParameter(reader, spectrum.Parameters[g].Components[c][p], status);
                                    else
                                        ReadParameter(reader, null, 0);
                        } else {
                            reader.ReadBytes(6 * cc * cs);
                            //position += (sizeof(float) + sizeof(byte) + sizeof(byte)) * cc * cs;
                        }
                    }
                } catch (EndOfStreamException) {
                } finally {
                    reader.Close();
                }
            }
        }

        #region Parameters IO
        /// | float | byte   | byte  |
        /// |-------|--------|-------|
        /// | value | status | ref   |

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="parameter"></param>
        private void WriteParameter(BinaryWriter writer, IParameter parameter) {
            if (parameter != null) {
                if ((parameter.Definition.Properties & (ParameterProperties.Readonly | ParameterProperties.Hidden)) == 0) {
                    writer.Write((float)parameter.Value);
                    writer.Write((byte)parameter.Status);
                    writer.Write((byte)parameter.ReferenceGroup);
                }
            } else {
                writer.Write(-1.0f);
                writer.Write((byte)0);
                writer.Write((byte)0);
            }
        }

        private void ReadParameter(BinaryReader reader, IParameter parameter, ParameterStatus status) {
            //float value = reader.ReadSingle();
            //ParameterStatus rstatus = (ParameterStatus)reader.ReadByte();
            //int referenceGroup = reader.ReadByte();
            if (parameter != null) {
                if ((parameter.Definition.Properties & (ParameterProperties.Readonly | ParameterProperties.Hidden)) == 0) {
                    float value = reader.ReadSingle();
                    ParameterStatus rstatus = (ParameterStatus)reader.ReadByte();
                    int referenceGroup = reader.ReadByte();
                    if ((parameter.Status & status) == status) {
                        parameter.Value = value;
                        parameter.Status = rstatus;
                        parameter.ReferenceGroup = referenceGroup;
                    }
                }
            } else 
                reader.ReadBytes(6);
        }

        #endregion Parameters IO

        #endregion Data IO

    }
}
