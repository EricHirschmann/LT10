using System;
using System.Collections.Generic;
using System.Text;
using Evel.interfaces;
using Evel.engine;
using System.Xml;

namespace Evel.engine.anh {
    public class AnhSpectrum : SpectrumBase, ISpectrum {

        public AnhSpectrum(string path, ISpectraContainer container, int bufferStart)
            : base(path, container, bufferStart) {
        }

        public AnhSpectrum(XmlReader spectrumReader, string root, ISpectraContainer container, int bufferStart)
            : base(spectrumReader, root, container, bufferStart) {
        }

        /// <summary>
        /// Procedura przygotowująca parametry widma do odpowiedniego typu procedury minimalizacyjnej
        /// - Pole pod krzywą w zależności od wybranego zakresu
        /// - Intensywności funkcji zdolności rozdzielczej
        /// </summary>
        /// <param name="sm"></param>
        public override void prepareToSearch(SearchLevel sl, PrepareOptions po) {
            int i, c, g;
            ParameterStatus status = ParameterStatus.Free;
            switch (sl) {
                case SearchLevel.Local: status |= ParameterStatus.Local; break;
                case SearchLevel.Global: status |= ParameterStatus.Common; break;
            }
            if ((po & PrepareOptions.GlobalArea) == PrepareOptions.GlobalArea) {
                //pole pod krzywą eksperymentalną w wybranym zakresie
                this._rangeArea = 0;
                //for (i = 0; i < this._experimentalSpectrum.Length-1; i++)
                //    this._rangeArea += this._experimentalSpectrum[i];
                //this._rangeArea -= (int)((this._experimentalSpectrum.Length - 2) * _parameters[4].Components[0][3].Value);

                for (i = this._dataBufferStart; i <= this._dataBufferStop; i++)
                    this._rangeArea += this._container.Data[i];
                this._rangeArea -= (int)(this._dataLength * _parameters[4].Components[0][3].Value);

            }
            if ((po & PrepareOptions.ComponentIntensities) == PrepareOptions.ComponentIntensities) {
                for (g = 1; g < 3; g++) {
                    if (_parameters[g].Components.Size > 0) {
                        if ((_parameters[g].Components[0][0].Status & status) == status && _parameters[g].Components.IntensitiesState != ComponentIntsState.PreparedToSearch) {
                            _parameters[g].Components.IntensitiesState = ComponentIntsState.PreparedToSearch;
                            for (c = 1; c < _parameters[g].Components.Size; c++)
                                if ((_parameters[g].Components[c][0].Status & status) == status) // && !_parameters[g].Components[c][0].HasReferenceValue)
                                    _parameters[g].Components[c][0].Value = Math.Sqrt(Math.Abs(_parameters[g].Components[c][0].Value / _parameters[g].Components[0][0].Value));
                            
                        }
                        //if ((_parameters[g].Components[0][0].Status & status) == status)
                            _parameters[g].Components[0][0].Value = 1.0;
                    }
                }
                if ((((ContributedGroup)_parameters[2]).contribution.Status & status) == status && !((ContributedGroup)_parameters[2]).contribution.HasReferenceValue)
                    ((ContributedGroup)_parameters[2]).contribution.Value = 1 / (1 / ((ContributedGroup)_parameters[2]).contribution.Value - 1);
            }
            if ((po & PrepareOptions.PromptIntensities) == PrepareOptions.PromptIntensities) {
                //intensywności funkcji zdolności rozdzielczej. Muszą być tak przekonwertowane, by
                //pierwszy wyraz był równy 1
                if (_parameters[3].Components.Size > 0) {
                    if ((_parameters[3].Components[0][0].Status & status) == status && _parameters[3].Components.IntensitiesState != ComponentIntsState.PreparedToSearch) {
                        _parameters[3].Components.IntensitiesState = ComponentIntsState.PreparedToSearch;
                        for (c = 1; c < _parameters[3].Components.Size; c++)
                            if ((_parameters[3].Components[c][0].Status & status) == status) // && !_parameters[3].Components[c][0].HasReferenceValue)
                                _parameters[3].Components[c][0].Value = Math.Sqrt(_parameters[3].Components[c][0].Value / _parameters[3].Components[0][0].Value);
                    }
                    //if ((_parameters[3].Components[0][0].Status & status) == status)
                        _parameters[3].Components[0][0].Value = 1.0;
                }
            }
        }

        public override void normalizeAfterSearch(SearchLevel sl, PrepareOptions po, bool flagOnly) {
            int gid, cid;
            double sum, ifree;
            ParameterStatus status = ParameterStatus.Free;
            switch (sl) {
                case SearchLevel.Local: status |= ParameterStatus.Local; break;
                case SearchLevel.Global: status |= ParameterStatus.Common; break;
                //case SearchLevel.Preliminary: status |= ParameterStatus.Local | ParameterStatus.Common; break;
            }
            //sample, source, prompt
            for (gid = 1; gid < 4; gid++) {
                if (_parameters[gid].Components.Size == 0) continue;
                if (gid < 3 && (po & PrepareOptions.ComponentIntensities) == 0) continue;
                if (gid == 3 && (po & PrepareOptions.PromptIntensities) == 0) continue;
                sum = 1;
                ifree = 1;
                if (_parameters[gid].Components.IntensitiesState == ComponentIntsState.PreparedToSearch) {
                    if ((_parameters[gid].Components[0][0].Status & status) == status) {
                        if (!flagOnly) {
                            for (cid = 1; cid < _parameters[gid].Components.Size; cid++)
                                if ((_parameters[gid].Components[cid][0].Status & ParameterStatus.Fixed) > 0)
                                    ifree -= _parameters[gid].Components[cid][0].Value;
                                else
                                    sum += _parameters[gid].Components[cid][0].Value * _parameters[gid].Components[cid][0].Value;
                            for (cid = 0; cid < _parameters[gid].Components.Size; cid++)
                                if (((_parameters[gid].Components[cid][0].Status & ParameterStatus.Free) > 0)) // && !_parameters[gid].Components[cid][0].HasReferenceValue)
                                    _parameters[gid].Components[cid][0].Value = ifree * _parameters[gid].Components[cid][0].Value * _parameters[gid].Components[cid][0].Value / sum;
                        }
                        _parameters[gid].Components.IntensitiesState = ComponentIntsState.Normed;
                    }
                }
            }
            if ((po & PrepareOptions.ComponentIntensities) > 0) {
                if ((((ContributedGroup)_parameters[2]).contribution.Status & status) == status && !((ContributedGroup)_parameters[2]).contribution.HasReferenceValue)
                    ((ContributedGroup)_parameters[2]).contribution.Value = Math.Abs(((ContributedGroup)_parameters[2]).contribution.Value) / (1 + Math.Abs(((ContributedGroup)_parameters[2]).contribution.Value));
            }
        }
    }
}
