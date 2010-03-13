﻿// <copyright file="TobiiSetting.cs" company="FU Berlin">
// ******************************************************
// OGAMA - open gaze and mouse analyzer 
// Copyright (C) 2010 Adrian Voßkühler  
// ------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation; either version 2 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with this program; if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// **************************************************************
// </copyright>
// <author>Adrian Voßkühler</author>
// <email>adrian.vosskuehler@fu-berlin.de</email>

#if TOBII

namespace Ogama.Modules.Recording.Tobii
{
  using System;
  using System.Collections.Generic;
  using System.Drawing;
  using System.Text;
  using System.Xml.Serialization;

  using TetComp;
  using VectorGraphics;
  using VectorGraphics.CustomTypeConverter;

  /// <summary>
  /// Class to save settings for the tobii eye tracking system.
  /// It is XML serializable and can be stored in a file via 
  /// the <see cref="XmlSerializer"/> class.
  /// <example>XmlSerializer serializer = new XmlSerializer(typeof(TobiiSetting));</example>
  /// </summary>
  [Serializable]
  public class TobiiSetting
  {
    ///////////////////////////////////////////////////////////////////////////////
    // Defining Constants                                                        //
    ///////////////////////////////////////////////////////////////////////////////
    #region CONSTANTS
    #endregion //CONSTANTS

    ///////////////////////////////////////////////////////////////////////////////
    // Defining Variables, Enumerations, Events                                  //
    ///////////////////////////////////////////////////////////////////////////////
    #region FIELDS

    /// <summary>
    /// Saves the server adress of the tobii tracking system.
    /// </summary>
    private string tetServerAddress;

    /// <summary>
    /// Saves the port for the server adress of the tobii tracking system.
    /// </summary>
    private int tetServerPort;

    /// <summary>
    /// Saves the number of calibration points.
    /// </summary>
    private TetNumCalibPoints tetNumCalibPoints;

    /// <summary>
    /// Saves the calibration point size.
    /// </summary>
    private TetCalibPointSize tetCalibPointSize;

    /// <summary>
    /// Saves the calibration speed.
    /// </summary>
    private TetCalibPointSpeed tetCalibPointSpeed;

    /// <summary>
    /// Saves the color for the calibration points.
    /// </summary>
    private Color tetPointColor;

    /// <summary>
    /// Saves the background color of the calibration procedure.
    /// </summary>
    private Color tetBackgroundColor;

    /// <summary>
    /// Saves the flag, whether the calibration point order should 
    /// be randomized.
    /// </summary>
    private bool tetRandomizeCalibPointOrder;

    #endregion //FIELDS

    ///////////////////////////////////////////////////////////////////////////////
    // Construction and Initializing methods                                     //
    ///////////////////////////////////////////////////////////////////////////////
    #region CONSTRUCTION

    /// <summary>
    /// Initializes a new instance of the TobiiSetting class.
    /// </summary>
    public TobiiSetting()
    {
      this.tetServerAddress = "169.254.4.217";
      this.tetServerPort = (int)TetConstants.TetConstants_DefaultServerPort;
      this.tetNumCalibPoints = TetNumCalibPoints.TetNumCalibPoints_5;
      this.tetCalibPointSize = TetCalibPointSize.TetCalibPointSize_Medium;
      this.tetCalibPointSpeed = TetCalibPointSpeed.TetCalibPointSpeed_MediumSlow;
      this.tetPointColor = Color.Red;
      this.tetBackgroundColor = SystemColors.ControlLight;
      this.tetRandomizeCalibPointOrder = true;
    }

    #endregion //CONSTRUCTION

    ///////////////////////////////////////////////////////////////////////////////
    // Defining Properties                                                       //
    ///////////////////////////////////////////////////////////////////////////////
    #region PROPERTIES

    /// <summary>
    /// Gets or sets the TETServer IP host address.
    /// </summary>
    /// <value>A <see cref="string"/> with the server address.</value>
    /// <remarks>This can be detected via a ping to the serial number in the command line.</remarks>
    public string TetServerAddress
    {
      get { return this.tetServerAddress; }
      set { this.tetServerAddress = value; }
    }

    /// <summary>
    /// Gets or sets the TETServer IP port number.
    /// </summary>
    /// <value>A <see cref="int"/> with the port number, default is 4455.</value>
    public int TetServerPort
    {
      get { return this.tetServerPort; }
      set { this.tetServerPort = value; }
    }

    /// <summary>
    /// Gets or sets the number of calibration points that are shown.
    /// </summary>
    /// <remarks>5 are standard, 9 gives higher precision, 2 are for uncooperative subjects.</remarks>
    /// <value>A <see cref="TetNumCalibPoints"/> with the number of points, can be  (2,5,9).</value>
    public TetNumCalibPoints TetNumCalibPoint
    {
      get { return this.tetNumCalibPoints; }
      set { this.tetNumCalibPoints = value; }
    }

    /// <summary>
    /// Gets or sets the size of the calibration point to change its place.
    /// </summary>
    /// <value>A <see cref="TetCalibPointSize"/> which is small, medium or large.</value>
    public TetCalibPointSize TetCalibPointSizes
    {
      get { return this.tetCalibPointSize; }
      set { this.tetCalibPointSize = value; }
    }

    /// <summary>
    /// Gets or sets the speed of the Calibration point.
    /// </summary>
    /// <value>A <see cref="TetCalibPointSpeed"/> which is between fast and slow.</value>
    public TetCalibPointSpeed TetCalibPointSpeeds
    {
      get { return this.tetCalibPointSpeed; }
      set { this.tetCalibPointSpeed = value; }
    }

    /// <summary>
    /// Gets or sets the color of the circles marking the calibration points.
    /// The default value is red.
    /// </summary>
    /// <value>A <see cref="Color"/> for the calibration points,
    /// which is converted into a OLE_COLOR Structure.</value>
    [XmlIgnoreAttribute()]
    public Color TetCalibPointColor
    {
      get { return this.tetPointColor; }
      set { this.tetPointColor = value; }
    }

    /// <summary>
    /// Gets or sets the <see cref="SerializedTetCalibPointColor"/>.
    /// Serializes and deserializes the <see cref="TetCalibPointColor"/> to XML,
    /// because XMLSerializer can not serialize <see cref="Color"/> values.
    /// </summary>
    public string SerializedTetCalibPointColor
    {
      get { return ObjectStringConverter.ColorToHtmlAlpha(this.tetPointColor); }
      set { this.tetPointColor = ObjectStringConverter.HtmlAlphaToColor(value); }
    }

    /// <summary>
    /// Gets or sets the color of the calibration window background.
    /// </summary>
    /// <value>A <see cref="Color"/> for the background,
    /// which is converted into a OLE_COLOR Structure.</value>
    [XmlIgnoreAttribute()]
    public Color TetCalibBackgroundColor
    {
      get { return this.tetBackgroundColor; }
      set { this.tetBackgroundColor = value; }
    }

    /// <summary>
    /// Gets or sets the <see cref="SerializedTetCalibBackgroundColor"/>.
    /// Serializes and deserializes the <see cref="TetCalibBackgroundColor"/> to XML,
    /// because XMLSerializer can not serialize <see cref="Color"/> values.
    /// </summary>
    public string SerializedTetCalibBackgroundColor
    {
      get { return ObjectStringConverter.ColorToHtmlAlpha(this.tetBackgroundColor); }
      set { this.tetBackgroundColor = ObjectStringConverter.HtmlAlphaToColor(value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether calibration point order should be randomized or not.
    /// </summary>
    /// <value>A <see cref="Boolean"/> indicating the randomizing of the calibration points.</value>
    public bool TetRandomizeCalibPointOrder
    {
      get { return this.tetRandomizeCalibPointOrder; }
      set { this.tetRandomizeCalibPointOrder = value; }
    }

    #endregion //PROPERTIES

    ///////////////////////////////////////////////////////////////////////////////
    // Eventhandler                                                              //
    ///////////////////////////////////////////////////////////////////////////////
    #region EVENTS

    ///////////////////////////////////////////////////////////////////////////////
    // Eventhandler for UI, Menu, Buttons, Toolbars etc.                         //
    ///////////////////////////////////////////////////////////////////////////////
    #region WINDOWSEVENTHANDLER
    #endregion //WINDOWSEVENTHANDLER

    ///////////////////////////////////////////////////////////////////////////////
    // Eventhandler for Custom Defined Events                                    //
    ///////////////////////////////////////////////////////////////////////////////
    #region CUSTOMEVENTHANDLER
    #endregion //CUSTOMEVENTHANDLER

    #endregion //EVENTS

    ///////////////////////////////////////////////////////////////////////////////
    // Methods and Eventhandling for Background tasks                            //
    ///////////////////////////////////////////////////////////////////////////////
    #region BACKGROUNDWORKER
    #endregion //BACKGROUNDWORKER

    ///////////////////////////////////////////////////////////////////////////////
    // Inherited methods                                                         //
    ///////////////////////////////////////////////////////////////////////////////
    #region OVERRIDES
    #endregion //OVERRIDES

    ///////////////////////////////////////////////////////////////////////////////
    // Methods for doing main class job                                          //
    ///////////////////////////////////////////////////////////////////////////////
    #region METHODS
    #endregion //METHODS

    ///////////////////////////////////////////////////////////////////////////////
    // Small helping Methods                                                     //
    ///////////////////////////////////////////////////////////////////////////////
    #region HELPER
    #endregion //HELPER
  }
}

#endif