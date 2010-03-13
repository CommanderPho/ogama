﻿// <copyright file="PresenterModule.cs" company="FU Berlin">
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

namespace Ogama.Modules.Recording
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Data;
  using System.Diagnostics;
  using System.Drawing;
  using System.IO;
  using System.Text;
  using System.Threading;
  using System.Windows.Forms;

  using DirectX.Capture;
  using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;

  using Ogama.ExceptionHandling;
  using Ogama.Modules.Common;
  using Ogama.Modules.Recording.Presenter;
  using Ogama.Modules.SlideshowDesign;
  using OgamaControls;

  using VectorGraphics.Controls;
  using VectorGraphics.Elements;
  using VectorGraphics.StopConditions;
  using VectorGraphics.Tools;
  using VectorGraphics.Triggers;

  /// <summary>
  /// A <see cref="Form"/> that is used for stimuli presentation. 
  /// This class presentates the stimuli created by the stimulus creation form
  /// and stored in the <see cref="Document"/> <see cref="Properties.ExperimentSettings"/> member.
  /// </summary>
  public partial class PresenterModule : Form
  {
    ///////////////////////////////////////////////////////////////////////////////
    // Defining Constants                                                        //
    ///////////////////////////////////////////////////////////////////////////////
    #region CONSTANTS

    /// <summary>
    /// Specify the minimum intervall in milliseconds that should elapse before the next key
    /// press is accepted and forwarded to the stopcondition test.
    /// </summary>
    private const int MINIMUMKEYPRESSINTERVALLMS = 50;

    #endregion //CONSTANTS

    ///////////////////////////////////////////////////////////////////////////////
    // Defining Variables, Enumerations, Events                                  //
    ///////////////////////////////////////////////////////////////////////////////
    #region FIELDS

    /// <summary>
    /// A <see cref="Webcam"/> that controls the user camera
    /// if it is used.
    /// </summary>
    private Webcam userCamera;

    /// <summary>
    /// The <see cref="RecordModule.GetTimeDelegate"/> which 
    /// can be called to retreive the current sample time
    /// from the recorder.
    /// </summary>
    private RecordModule.GetTimeDelegate getTimeMethod;

    /// <summary>
    /// The <see cref="Control"/> in which the preview of the user
    /// camera should be shown. By default this is the webcam control
    /// of the record module.
    /// </summary>
    private Control userCameraPreviewWindow;

    /// <summary>
    /// A <see cref="CaptureDeviceProperties"/> describing
    /// the webcam properties (capture filter, compressor, etc)
    /// </summary>
    private CaptureDeviceProperties userCameraProperties;

    /// <summary>
    /// This <see cref="Control"/> can contain the window in which 
    /// the screen capturing is preview during capturing of flash windows.
    /// </summary>
    private Control screenCapturePreviewWindow;

    /// <summary>
    ///  The main <see cref="BufferedGraphicsContext"/> which is used 
    ///  to allocate the <see cref="BufferedGraphics"/> for the 
    ///  <see cref="SlidePresentationContainer"/>s.
    /// </summary>
    private BufferedGraphicsContext context;

    /// <summary>
    /// Saves the bounds of the presentation screen for
    /// quick access.
    /// </summary>
    private Rectangle presentationBounds;

    /// <summary>
    /// An optional trigger that can be send for each slide additionally to the 
    /// triggers that can be defined for each slide separately.
    /// </summary>
    private Trigger generalTrigger;

    /// <summary>
    /// Indicates sending of triggers.
    /// This value is valid for the slide triggers and the general trigger.
    /// </summary>
    private bool enableTrigger;

    /// <summary>
    /// Saves the list of trials to display
    /// </summary>
    private TrialCollection trials;

    /// <summary>
    /// The trial counter.
    /// </summary>
    private int trialCounter;

    /// <summary>
    /// The slide counter.
    /// </summary>
    private int slideCounter;

    /// <summary>
    /// Saves the currently pressed mouse button.
    /// </summary>
    private MouseButtons currentMousebutton;

    /// <summary>
    /// Saves the currently pressed key
    /// </summary>
    private Keys currentKey;

    /// <summary>
    /// A precise timer.
    /// </summary>
    private Stopwatch watch;

    /// <summary>
    /// Flag that is true if the cursor should be hidden.
    /// </summary>
    private bool hiddenCursor;

    /// <summary>
    /// Indicates the closing of the form.
    /// </summary>
    private bool closing;

    /// <summary>
    /// This member indicates the currently shown <see cref="SlidePresentationContainer"/>
    /// that can be preparedSlideOne or preparedSlideTwo
    /// </summary>
    private ShownContainer shownContainer;

    /// <summary>
    /// Contains the currently shown <see cref="SlidePresentationContainer"/>
    /// that is either preparedSlideOne or preparedSlideTwo.
    /// </summary>
    private SlidePresentationContainer shownSlide;

    /// <summary>
    /// The first of the both available <see cref="SlidePresentationContainer"/>
    /// that are used to prepare and present slides on time.
    /// </summary>
    private SlidePresentationContainer preparedSlideOne;

    /// <summary>
    /// The second of the both available <see cref="SlidePresentationContainer"/>
    /// that are used to prepare and present slides on time.
    /// </summary>
    private SlidePresentationContainer preparedSlideTwo;

    #endregion //FIELDS

    ///////////////////////////////////////////////////////////////////////////////
    // Construction and Initializing methods                                     //
    ///////////////////////////////////////////////////////////////////////////////
    #region CONSTRUCTION

    /// <summary>
    /// Initializes a new instance of the PresenterModule class.
    /// </summary>
    public PresenterModule()
    {
      this.InitializeComponent();

      // Retrieves the BufferedGraphicsContext for the 
      // current application domain.
      this.context = BufferedGraphicsManager.Current;

      // Sets the maximum size for the primary graphics buffer
      // of the buffered graphics context for the application
      // domain.  Any allocation requests for a buffer larger 
      // than this will create a temporary buffered graphics 
      // context to host the graphics buffer.
      this.context.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);

      this.preparedSlideOne = new SlidePresentationContainer();
      this.preparedSlideOne.ContainerControl = this.panelOne;

      // Allocates a graphics buffer the size of this form
      // using the pixel format of the Graphics created by 
      // the Form.CreateGraphics() method, which returns a 
      // Graphics object that matches the pixel format of the form.
      int width = Document.ActiveDocument.ExperimentSettings.WidthStimulusScreen;
      int height = Document.ActiveDocument.ExperimentSettings.HeightStimulusScreen;

      this.preparedSlideOne.DrawingSurface = this.context.Allocate(
        this.panelOne.CreateGraphics(),
        new Rectangle(0, 0, width, height));
      this.panelOne.DrawingSurface = this.preparedSlideOne.DrawingSurface;

      this.preparedSlideTwo = new SlidePresentationContainer();
      this.preparedSlideTwo.ContainerControl = this.panelTwo;
      this.preparedSlideTwo.DrawingSurface = this.context.Allocate(
        this.panelTwo.CreateGraphics(),
        new Rectangle(0, 0, width, height));
      this.panelTwo.DrawingSurface = this.preparedSlideTwo.DrawingSurface;
    }

    #endregion //CONSTRUCTION

    ///////////////////////////////////////////////////////////////////////////////
    // Defining Enumerations                                                     //
    ///////////////////////////////////////////////////////////////////////////////
    #region ENUMS

    /// <summary>
    /// The delegate for the call to SendTrigger from a thread pool thread.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// to send the trigger for.</param>
    private delegate void SendTriggerDelegate(SlidePresentationContainer slideContainer);

    /// <summary>
    /// The delegate for the call to prepare the screen capture from a thread pool thread.
    /// </summary>
    /// <param name="shownTrialCounter">The trial counter ID to be shown.</param>
    private delegate void PrepareScreenCaptureDelegate(int shownTrialCounter);

    /// <summary>
    /// Event. Raised when slide has changed.
    /// Used for asynchronous calls.
    /// </summary>
    public event SlideChangedEventHandler SlideChanged;

    /// <summary>c
    /// Event. Raised when trial has changed.
    /// Used for asynchronous calls.
    /// </summary>
    public event TrialChangedEventHandler TrialChanged;

    /// <summary>
    /// Event. Raised when slide or trial counter has changed.
    /// Used for immediated calls.
    /// </summary>
    public event CounterChangedEventHandler CounterChanged;

    /// <summary>
    /// Event. Raised when trial event occured.
    /// Used for asynchronous calls.
    /// </summary>
    public event TrialEventOccuredEventHandler TrialEventOccured;

    /// <summary>
    /// Event. Raisen when presentation has finished or is aborted.
    /// Used for asynchronous calls.
    /// </summary>
    public event EventHandler PresentationDone;

    /// <summary>
    /// This enumeration describes which <see cref="SlidePresentationContainer"/>
    /// is currently beeing displayed. Can be None, One or Two.
    /// </summary>
    private enum ShownContainer
    {
      /// <summary>
      /// No container is shown.
      /// </summary>
      None,

      /// <summary>
      /// The container preparedSlideOne is shown.
      /// </summary>
      One,

      /// <summary>
      /// The container preparedSlideTwo is shown.
      /// </summary>
      Two,
    }

    #endregion ENUMS

    ///////////////////////////////////////////////////////////////////////////////
    // Defining Properties                                                       //
    ///////////////////////////////////////////////////////////////////////////////
    #region PROPERTIES

    /// <summary>
    /// Gets or sets the list of trials that should be presented.
    /// </summary>
    /// <value>A <see cref="TrialCollection"/> with the slides to present.</value>
    public TrialCollection TrialList
    {
      get { return this.trials; }
      set { this.trials = value; }
    }

    /// <summary>
    /// Sets an optional trigger that can be send for each slide additionally to the 
    /// triggers that can be defined for each slide separately.
    /// </summary>
    public Trigger GeneralTrigger
    {
      set { this.generalTrigger = value; }
    }

    /// <summary>
    /// Sets a value indicating whether to send triggers.
    /// This value is valid for the slide triggers and the general trigger.
    /// </summary>
    public bool EnableTrigger
    {
      set { this.enableTrigger = value; }
    }

    /// <summary>
    /// Sets the properties of the screen capture
    /// device.
    /// </summary>
    public CaptureDeviceProperties CaptureProperties
    {
      set
      {
        this.preparedSlideOne.InitializeScreenCapture(value);
        this.preparedSlideTwo.InitializeScreenCapture(value);
      }
    }

    /// <summary>
    /// Sets the <see cref="Webcam"/> usercamera.
    /// </summary>
    public CaptureDeviceProperties UserCameraProperties
    {
      set { this.userCameraProperties = value; }
    }

    /// <summary>
    /// Sets the preview window control for the usercamera
    /// during recording.
    /// </summary>
    public Control UserCameraPreviewWindow
    {
      set { this.userCameraPreviewWindow = value; }
    }

    /// <summary>
    /// Sets the preview window control for the screen capture
    /// during recording.
    /// </summary>
    public Control ScreenCapturePreviewWindow
    {
      set { this.screenCapturePreviewWindow = value; }
    }

    /// <summary>
    /// Sets the  <see cref="RecordModule.GetTimeDelegate"/> which 
    /// can be called to retreive the current sample time
    /// from the recorder.
    /// </summary>
    public RecordModule.GetTimeDelegate GetTimeMethod
    {
      set { this.getTimeMethod = value; }
    }

    /// <summary>
    /// Gets the <see cref="DXCapture"/> with the current
    /// screen capture object or null if none is 
    /// capturing.
    /// </summary>
    public DXCapture CurrentScreenCapture
    {
      get
      {
        switch (this.shownContainer)
        {
          case ShownContainer.One:
            if (this.preparedSlideOne.ScreenCapture.Capturing)
            {
              return this.preparedSlideOne.ScreenCapture;
            }

            break;
          case ShownContainer.Two:
            if (this.preparedSlideTwo.ScreenCapture.Capturing)
            {
              return this.preparedSlideTwo.ScreenCapture;
            }

            break;
        }

        return null;
      }
    }

    #endregion //PROPERTIES

    ///////////////////////////////////////////////////////////////////////////////
    // Public methods                                                            //
    ///////////////////////////////////////////////////////////////////////////////
    #region PUBLICMETHODS

    /// <summary>
    /// This method stops the timer, raises the <see cref="PresentationDone"/>
    /// event and ends the presentation by closing this form.
    /// </summary>
    /// <param name="sendBreakTrigger"><strong>True</strong>
    /// if this call to EndPresentation is due to a break of
    /// the presentation via ESC or from record module.</param>
    public void EndPresentation(bool sendBreakTrigger)
    {
      this.closing = true;

      if (sendBreakTrigger)
      {
        long webcamTime = this.userCamera != null ? this.userCamera.GetCurrentTime() : -1;

        this.OnCounterChanged(new CounterChangedEventArgs(
          -5,
          this.slideCounter));

        this.OnTrialChanged(new TrialChangedEventArgs(
          this.shownSlide.Trial,
          null,
          new KeyStopCondition(Keys.Escape, false, null),
          this.shownSlide.Slide.Category,
          -1,
          webcamTime));
      }

      if (this.hiddenCursor)
      {
        Cursor.Show();
      }

      this.trials.Clear();

      this.DisposeSlideContainer(this.preparedSlideOne);
      this.DisposeSlideContainer(this.preparedSlideTwo);

      this.preparedSlideOne.DrawingSurface.Dispose();
      this.preparedSlideTwo.DrawingSurface.Dispose();

      if (this.preparedSlideOne.ScreenCapture != null)
      {
        this.preparedSlideOne.ScreenCapture.Dispose();
      }

      if (this.preparedSlideOne.ScreenCapture != null)
      {
        this.preparedSlideTwo.ScreenCapture.Dispose();
      }

      if (this.userCamera != null)
      {
        if (this.userCamera.Properties.CaptureMode != CaptureMode.None)
        {
          // Stop UserCamera
          this.userCamera.StopCapture();
        }

        this.userCamera.Preview = false;
        this.userCamera.Dispose();
      }

      this.OnPresentationDone(EventArgs.Empty);

      try
      {
        this.context.Invalidate();
      }
      catch (Exception ex)
      {
        bool rethrow = ExceptionPolicy.HandleException(ex, "Log Only Policy");
        if (rethrow)
        {
          throw;
        }
      }

      if (this.InvokeRequired)
      {
        MethodInvoker closeMethod = new MethodInvoker(this.Close);
        this.Invoke(closeMethod);
        return;
      }

      this.Close();
    }

    #endregion //PUBLICMETHODS

    ///////////////////////////////////////////////////////////////////////////////
    // Inherited methods                                                         //
    ///////////////////////////////////////////////////////////////////////////////
    #region OVERRIDES

    /// <summary>
    /// Overriden <see cref="ProcessCmdKey(ref Message,Keys)"/> method. 
    /// Captures all pressed keys including
    /// Alt, Ctrl, Space, Esc that are normally not raised as KeyDown in a form.
    /// </summary>
    /// <param name="msg">The msg parameter contains the Windows Message, such as WM_KEYDOWN</param>
    /// <param name="keyData">The keyData parameter contains the key code of the key that was pressed. If CTRL or ALT was also pressed, the keyData parameter contains the ModifierKey information.</param>
    /// <returns>True if Key should be processed ?</returns>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      const int WM_KEYDOWN = 0x100;
      const int WM_SYSKEYDOWN = 0x104;

      if (((msg.Msg == WM_KEYDOWN) || (msg.Msg == WM_SYSKEYDOWN)) && (!this.closing))
      {
        long eventTime = -1;
        if (this.getTimeMethod != null)
        {
          eventTime = this.getTimeMethod();
        }

        if (this.watch.ElapsedMilliseconds > MINIMUMKEYPRESSINTERVALLMS)
        {
          // Check for markers
          if (keyData == Keys.F12)
          {
            // Store marker event
            MediaEvent keyEvent = new MediaEvent();
            keyEvent.Type = EventType.Marker;
            keyEvent.Task = MediaEventTask.None;
            keyEvent.Param = string.Empty;
            this.OnTrialEventOccured(new TrialEventOccuredEventArgs(keyEvent, eventTime));
          }
          else
          {
            this.currentKey = keyData;

            if (!this.CheckforSlideChange(false))
            {
              // Store key event only when slide has not changed
              // otherwise the event will be stored during
              // trialchanged event.
              InputEvent keyEvent = new InputEvent();
              keyEvent.Type = EventType.Key;
              keyEvent.Task = InputEventTask.Down;
              KeyStopCondition ksc = new KeyStopCondition(keyData, false, null);
              keyEvent.Param = ksc.ToString();
              this.OnTrialEventOccured(new TrialEventOccuredEventArgs(keyEvent, eventTime));
            }

            this.watch.Reset();
            this.watch.Start();
          }
        }
      }

      return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// This method asynchronously calls the drawing method of the slide of
    /// the given container to the given slidecontainers drawing surface.
    /// </summary>
    /// <param name="slideToDraw">The <see cref="SlidePresentationContainer"/>
    /// whichs slide should be drawn to its buffer.</param>
    private void DrawToBuffer(SlidePresentationContainer slideToDraw)
    {
      if (slideToDraw.Slide.PresentationSize == Size.Empty)
      {
        slideToDraw.Slide.PresentationSize = Document.ActiveDocument.PresentationSize;
      }

      // Draw slides contents.
      this.DrawSlideAsyncMethod(slideToDraw.Slide, slideToDraw.DrawingSurface.Graphics);
    }

    #endregion //OVERRIDES

    ///////////////////////////////////////////////////////////////////////////////
    // Eventhandler                                                              //
    ///////////////////////////////////////////////////////////////////////////////
    #region EVENTS

    ///////////////////////////////////////////////////////////////////////////////
    // Eventhandler for UI, Menu, Buttons, Toolbars etc.                         //
    ///////////////////////////////////////////////////////////////////////////////
    #region WINDOWSEVENTHANDLER

    /// <summary>
    /// The <see cref="Form.Load"/> event handler. 
    /// Initializes the form on the presentation screen,
    /// then initializes flash stimulus handling,
    /// starts the timing control and then
    /// loads first stimulus.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">An empty <see cref="EventArgs"/></param>
    private void PresenterModule_Load(object sender, EventArgs e)
    {
      try
      {
        if (this.trials == null || this.trials.Count == 0)
        {
          this.Close();
          return;
        }

        // Show presentation on secondary screen if possible,
        // otherwise maximise on primary screen
        PresentationScreen.PutFormOnPresentationScreen(this, true);

        // Some slides may have a hidden cursor.
        this.hiddenCursor = false;

        // Initializes the timer
        this.watch = new Stopwatch();
        this.watch.Start();

        // Start UserCamera
        this.InitializeUserCamera(this.userCameraProperties);
        if (this.userCamera != null && this.userCamera.Properties.CaptureMode != CaptureMode.None)
        {
          AsyncHelper.FireAsync(new MethodInvoker(this.userCamera.RunGraph));
        }

        this.presentationBounds = PresentationScreen.GetPresentationWorkingArea();

        // Loads first slide, or closes form if there are no slides to display.
        this.trialCounter = -1;
        this.InitializeFirstTrial();
      }
      catch (Exception ex)
      {
        bool rethrow = ExceptionPolicy.HandleException(ex, "Global Policy");
        if (rethrow)
        {
          throw;
        }

        this.Close();
      }
    }

    /// <summary>
    /// The <see cref="Form.FormClosing"/> event handler.
    /// Stops the stopwatch timer.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">A <see cref="FormClosingEventArgs"/> with the event data.</param>
    private void frmPresenter_FormClosing(object sender, FormClosingEventArgs e)
    {
      this.watch.Stop();
    }

    /// <summary>
    /// The <see cref="Control.KeyDown"/> event handler.
    /// The receiving of key press events has moved to
    /// <see cref="ProcessCmdKey"/> because this method
    /// allows logging of modifier keys also.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">A <see cref="KeyEventArgs"/> with the event data.</param>
    private void frmPresenter_KeyDown(object sender, KeyEventArgs e)
    {
      // _currentKey = (e.KeyData & e.KeyCode);
      // This has moved to ProcessCmdKey
    }

    /// <summary>
    /// The <see cref="Control.MouseDown"/> event handler.
    /// Sets the <see cref="currentMousebutton"/> and raises
    /// the <see cref="TrialEventOccured"/> event.
    /// </summary>
    /// <remarks>If the mouse button is a response that indicates
    /// a slide change, do it.</remarks>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">A <see cref="MouseEventArgs"/> with the event data.</param>
    private void frmPresenter_MouseDown(object sender, MouseEventArgs e)
    {
      this.currentMousebutton = e.Button;
      long eventTime = -1;
      if (this.getTimeMethod != null)
      {
        eventTime = this.getTimeMethod();
      }

      if (!this.CheckforSlideChange(false))
      {
        InputEvent mouseEvent = new InputEvent();
        mouseEvent.Type = EventType.Mouse;
        mouseEvent.Task = InputEventTask.Down;
        MouseStopCondition msc = new MouseStopCondition(e.Button, false, string.Empty, null, e.Location);
        mouseEvent.Param = msc.ToString();
        this.OnTrialEventOccured(new TrialEventOccuredEventArgs(mouseEvent, eventTime));

        this.CheckforAudioStimulusOnClick(this.shownSlide, e.Location, eventTime);
      }
    }

    /// <summary>
    /// The <see cref="Control.MouseUp"/> event handler.
    /// Resets the <see cref="currentMousebutton"/> to
    /// <see cref="MouseButtons.None"/>.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">A <see cref="MouseEventArgs"/> with the event data.</param>
    private void frmPresenter_MouseUp(object sender, MouseEventArgs e)
    {
      this.currentMousebutton = MouseButtons.None;
      long eventTime = -1;
      if (this.getTimeMethod != null)
      {
        eventTime = this.getTimeMethod();
      }

      InputEvent mouseEvent = new InputEvent();
      mouseEvent.Type = EventType.Mouse;
      mouseEvent.Task = InputEventTask.Up;
      MouseStopCondition msc = new MouseStopCondition(e.Button, false, string.Empty, null, e.Location);
      mouseEvent.Param = msc.ToString();
      this.OnTrialEventOccured(new TrialEventOccuredEventArgs(mouseEvent, eventTime));
    }

    #endregion //WINDOWSEVENTHANDLER

    ///////////////////////////////////////////////////////////////////////////////
    // Eventhandler for Custom Defined Events                                    //
    ///////////////////////////////////////////////////////////////////////////////
    #region CUSTOMEVENTHANDLER

    /// <summary>
    /// The <see cref="MultimediaTimer.Tick"/> event handler for the 
    /// <see cref="MultimediaTimer"/>.
    /// Is triggered whenever it is time to change the displayed slide.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">An empty <see cref="EventArgs"/></param>.
    private void timer_Tick(object sender, EventArgs e)
    {
      this.CheckforSlideChange(true);
    }

    /// <summary>
    /// This method raises the <see cref="TrialEventOccured"/> 
    /// event by invoking the delegates.
    /// It should be called whenever an event occured in a trial.
    /// </summary>
    /// <param name="e">A <see cref="TrialEventOccuredEventArgs"/> with the event data.</param>.
    private void OnTrialEventOccured(TrialEventOccuredEventArgs e)
    {
      if (this.TrialEventOccured != null)
      {
        AsyncHelper.FireAndForget(this.TrialEventOccured, this, e);
      }
    }

    /// <summary>
    /// This method raises the <see cref="CounterChanged"/> 
    /// event by invoking the delegates.
    /// It should be called when the current slide ot trial index has changed.
    /// </summary>
    /// <param name="e">A <see cref="CounterChangedEventArgs"/> with the event data.</param>.
    private void OnCounterChanged(CounterChangedEventArgs e)
    {
      if (this.CounterChanged != null)
      {
        // This is the only place we should not use asynchrous calls.
        this.CounterChanged(this, e);
      }
    }

    /// <summary>
    /// This method raises the <see cref="SlideChanged"/> 
    /// event by invoking the delegates.
    /// It should be called when the current slide has changed.
    /// </summary>
    /// <param name="e">A <see cref="SlideChangedEventArgs"/> with the event data.</param>.
    private void OnSlideChanged(SlideChangedEventArgs e)
    {
      if (this.SlideChanged != null)
      {
        AsyncHelper.FireAndForget(this.SlideChanged, this, e);
      }
    }

    /// <summary>
    /// This method raises the <see cref="TrialChanged"/> 
    /// event by invoking the delegates.
    /// It should be called when the current trial has changed.
    /// </summary>
    /// <param name="e">A <see cref="TrialChangedEventArgs"/> with the event data.</param>.
    private void OnTrialChanged(TrialChangedEventArgs e)
    {
      if (this.TrialChanged != null)
      {
        AsyncHelper.FireAndForget(this.TrialChanged, this, e);
      }
    }

    /// <summary>
    /// This method raises the <see cref="PresentationDone"/> 
    /// event by invoking the delegates.
    /// It should be called when the presentation has finished.
    /// </summary>
    /// <param name="e">An empty <see cref="EventArgs"/></param>.
    /// <remarks>We just fire the <see cref="PresentationDone"/> event and don´t wait
    /// for finish, because otherwise the last slide will stay visible until the whole
    /// writing to database is done.</remarks>
    private void OnPresentationDone(EventArgs e)
    {
      if (this.PresentationDone != null)
      {
        AsyncHelper.FireAndForget(this.PresentationDone, this, e);
      }
    }

    #endregion //CUSTOMEVENTHANDLER

    #endregion //EVENTS

    ///////////////////////////////////////////////////////////////////////////////
    // Methods and Eventhandling for Background tasks                            //
    ///////////////////////////////////////////////////////////////////////////////
    #region BACKGROUNDWORKER

    /// <summary>
    /// This method is the DoWork event handler for the thread
    /// that preparates the next slide to be displayed.
    /// </summary>
    /// <param name="data">An <see cref="Object"/> with the
    /// thread parameters.</param>
    private void PreparationThread_DoWork(object data)
    {
      List<object> threadParams = (List<object>)data;

      int trialCounter = (int)threadParams[0];
      int slideCounter = (int)threadParams[1];

      Trial shownTrial = this.trials[trialCounter];
      Slide shownSlide = shownTrial[slideCounter];

      if (shownTrial.IndexOf(shownSlide) == shownTrial.Count - 1)
      {
        // If trial consist of multiple slides then this was the last one,
        // Reset slide counter
        slideCounter = 0;

        // Increase trial counter
        trialCounter++;
      }
      else
      {
        // Increase slide counter
        slideCounter++;
      }

      this.PrepareSpecificSlide(trialCounter, slideCounter);
    }

    #endregion //BACKGROUNDWORKER

    ///////////////////////////////////////////////////////////////////////////////
    // Methods for doing main class job                                          //
    ///////////////////////////////////////////////////////////////////////////////
    #region METHODS

    /// <summary>
    /// This method parses the given <see cref="VGElement"/>
    /// for sound files and fills them in the <see cref="OgamaControls.AudioPlayer"/>.
    /// If they should be played on click, they are stored in the 
    /// <see cref="SlidePresentationContainer.ElementsWithAudioOnClick"/> list.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// this element belongs to.</param>
    /// <param name="element">The <see cref="VGElement"/> to search for audio content</param>
    private static void ParseElementForAudio(SlidePresentationContainer slideContainer, VGElement element)
    {
      if (element.Sound != null && element.Sound.ShouldPlay)
      {
        if (!element.Sound.ShowOnClick)
        {
          slideContainer.AudioPlayer.AddAudioChannel(element.Sound.FullFilename);
        }
        else
        {
          slideContainer.ElementsWithAudioOnClick.Add(element);
        }
      }
    }

    /// <summary>
    /// This method invokes the method to draw the given slide onto
    /// the given graphics asynchronously.
    /// That allows calling from a separate thread.
    /// </summary>
    /// <param name="slide">A <see cref="Slide"/> that should be drawn.</param>
    /// <param name="g">A <see cref="Graphics"/> on which the slide should be drawn.</param>
    private void DrawSlideAsyncMethod(Slide slide, Graphics g)
    {
      this.Invoke(new Slide.AsyncDrawSlideMethodCaller(Slide.DrawSlideAsync), slide, g);
    }

    /// <summary>
    /// This method parses the <see cref="Slide.VGStimuli"/>
    /// for elements with sound files and fills them in the <see cref="OgamaControls.AudioPlayer"/>.
    /// If they should be played on click, they are stored in the 
    /// <see cref="SlidePresentationContainer.ElementsWithAudioOnClick"/> list.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// to pe parsed for audio content that should be prepared for replay.
    /// </param>
    private void ParseElementsForAudio(SlidePresentationContainer slideContainer)
    {
      slideContainer.ElementsWithAudioOnClick.Clear();
      foreach (VGElement element in slideContainer.Slide.VGStimuli)
      {
        ParseElementForAudio(slideContainer, element);
      }

      foreach (VGElement element in slideContainer.Slide.ActiveXStimuli)
      {
        ParseElementForAudio(slideContainer, element);
      }
    }

    /// <summary>
    /// This method checks current response and timers for
    /// a value that indicates a slide change.
    /// It also aborts the presentation if the ESC key was pressed.
    /// </summary>
    /// <param name="timeOver">This flag indicates whether this
    /// method is called from the slide timer tick event, so the
    /// slide should definetely be changed.</param>
    /// <returns><strong>True</strong> if slide should be changed or presentation
    /// is done, otherwise <strong>false</strong>.</returns>
    private bool CheckforSlideChange(bool timeOver)
    {
      try
      {
        // Abort presentation, if ESC is pressed.
        if (this.currentKey == Keys.Escape)
        {
          this.EndPresentation(true);
          return true;
        }

        bool changeSlide = false;
        StopCondition response = null;
        if (!timeOver)
        {
          this.CheckResponses(this.shownSlide, out changeSlide, out response);
        }
        else
        {
          // if this method is called with timeover = true
          // the time condition is met, so change slide
          // without another test.
          changeSlide = true;
        }

        // go to next slide.
        if (changeSlide)
        {
          bool isLink = false;
          int newTrialID = -1;
          if (response != null)
          {
            this.CheckLinks(this.shownSlide, response, out isLink, out newTrialID);
          }

          bool trialChange = false;
          bool slideChange = false;

          Trial lastTrial = this.shownSlide.Trial;

          if (isLink)
          {
            int newTrialIndex = this.trials.GetIndexOfTrialByID(newTrialID);
            this.slideCounter = 0;
            this.trialCounter = newTrialIndex;
            trialChange = true;

            // Immediately prepare the link slide
            // because it could not be prepared in the 
            // default background preparation thread
            this.PrepareSpecificSlide(this.trialCounter, this.slideCounter);
          }
          else if (this.shownSlide.Trial.IndexOf(this.shownSlide.Slide) == this.shownSlide.Trial.Count - 1)
          {
            // If trial consist of multiple slides then this was the last one,
            // Reset slide counter
            this.slideCounter = 0;

            // Increase trial counter
            this.trialCounter++;
            trialChange = true;
          }
          else
          {
            // Increase slide counter
            this.slideCounter++;
            slideChange = true;
          }

          // If there is a screen capturing in progress
          // finish it in a new thread to let it
          // be saved on disk
          this.StopScreenCapturing(trialChange);

          // Switch to new slide/trial
          if (this.trialCounter < this.trials.Count - 1)
          {
            // Change the shown container which has
            // already a prepared slide in it
            this.PresentPreparedSlide();

            // Invoke the preparation of the next slide
            // in the trial list
            this.PrepareNextSlideAsynchronously(trialChange);
          }
          else if (this.trialCounter == this.trials.Count - 1)
          {
            // Now the last trial is going to be shown
            // so there is no slide left to be prepared
            this.PresentPreparedSlide();
          }
          else
          {
            // this was the last trial
            this.shownSlide.Trial = null;
          }

          // Save webcam time
          long webcamTime = this.userCamera != null ? this.userCamera.GetCurrentTime() : -1;

          // Send trigger in background thread
          AsyncHelper.FireAndForget(new SendTriggerDelegate(this.SendTrigger), this.shownSlide);

          // Send counter update in synchronous call
          int trialID = this.trialCounter < this.trials.Count ? this.trials[this.trialCounter].ID : -5;
          this.OnCounterChanged(new CounterChangedEventArgs(
            trialID,
            this.slideCounter));

          // Changes the mouse position and occurence according
          // to slide properties
          this.SetupMouse(this.shownSlide);

          // Send slide or trial change events in asynchronous calls.
          if (trialChange)
          {
            // TO DO : Currently joined slides will not have their own category
            // if displayed in a single trial.
            this.OnTrialChanged(new TrialChangedEventArgs(
              lastTrial,
              this.shownSlide.Trial,
              response,
              this.shownSlide.Slide.Category,
              this.trialCounter,
              webcamTime));
          }
          else if (slideChange)
          {
            // There are more slides in this trial, so only
            // raise OnSlideChanged
            this.OnSlideChanged(
              new SlideChangedEventArgs(
              lastTrial[this.slideCounter],
              response,
              this.slideCounter));
          }

          // Check for last trial
          if (this.trialCounter == this.trials.Count)
          {
            this.EndPresentation(false);
          }
          else
          {
            this.PlaySlideContainer(this.shownSlide);

          }

          return true;
        }
        else
        {
          return false;
        }
      }
      catch (Exception ex)
      {
        ExceptionMethods.ProcessUnhandledException(ex);
        return false;
      }
    }

    /// <summary>
    /// This method invokes the stop of current running
    /// screen capturings if applicable.
    /// </summary>
    /// <param name="trialChange">True, if the trial has changed,
    /// false if it should go on with the next slide in the trial.</param>
    private void StopScreenCapturing(bool trialChange)
    {
      switch (this.shownContainer)
      {
        case ShownContainer.One:
          // Stop capturing video
          if (this.preparedSlideOne.ScreenCapture != null && trialChange && this.preparedSlideOne.ScreenCapture.Capturing)
          {
            this.preparedSlideOne.ScreenCapture.StopAll();
            //AsyncHelper.FireAndForget(new MethodInvoker(this.preparedSlideOne.ScreenCapture.StopAll));
          }

          break;
        case ShownContainer.Two:
          // Stop capturing video
          if (this.preparedSlideTwo.ScreenCapture != null && trialChange && this.preparedSlideTwo.ScreenCapture.Capturing)
          {
            this.preparedSlideTwo.ScreenCapture.StopAll();
            //AsyncHelper.FireAndForget(new MethodInvoker(this.preparedSlideTwo.ScreenCapture.StopAll));
          }

          break;
      }
    }

    /// <summary>
    /// This method invokes the preparation of the next slides.
    /// </summary>
    /// <param name="trialChange">True, if the trial has changed,
    /// false if it should go on with the next slide in the trial.</param>
    private void PrepareNextSlideAsynchronously(bool trialChange)
    {
      // Initialize preparation thread
      List<object> threadParameters = new List<object>();
      threadParameters.Add(this.trialCounter);
      threadParameters.Add(this.slideCounter);

      Thread preparationThread = new Thread(new ParameterizedThreadStart(this.PreparationThread_DoWork));
      preparationThread.SetApartmentState(ApartmentState.STA);
      preparationThread.Start(threadParameters);

      if (trialChange)
      {
        AsyncHelper.FireAndForget(new PrepareScreenCaptureDelegate(this.PrepareScreenCapture), this.trialCounter);
      }
    }

    /// <summary>
    /// This method scans the next trial for flash content to initialize
    /// the corresponding screen capture device.
    /// </summary>
    /// <param name="shownTrialCounter">The zero-based counter of the
    /// current shown trial.</param>
    private void PrepareScreenCapture(int shownTrialCounter)
    {
      int nextTrialCounter = shownTrialCounter;
      nextTrialCounter++;
      Trial nextTrial = this.trials[nextTrialCounter];

      if (nextTrial.HasFlashContent)
      {
        string filename = Document.ActiveDocument.SelectionState.SubjectName +
         "-" + nextTrialCounter + ".avi";
        filename = Path.Combine(Document.ActiveDocument.ExperimentSettings.ThumbsPath, filename);
        switch (this.shownContainer)
        {
          case ShownContainer.One:
            // Prepare screen capturing
            if (this.preparedSlideTwo.ScreenCapture != null)
            {
              this.preparedSlideTwo.ScreenCapture.Filename = filename;
              //this.preparedSlideTwo.ScreenCapture.Cue();
            }

            break;
          case ShownContainer.Two:
            // Prepare screen capturing
            if (this.preparedSlideOne.ScreenCapture != null)
            {
              this.preparedSlideOne.ScreenCapture.Filename = filename;
              //this.preparedSlideOne.ScreenCapture.Cue();
            }

            break;
        }
      }
    }

    /// <summary>
    /// This method swaps the slide buffers by swapping the
    /// position of the controls panelOne and panelTwo and
    /// updating the shownSlide.
    /// </summary>
    private void PresentPreparedSlide()
    {
      switch (this.shownContainer)
      {
        case ShownContainer.One:
          this.Controls.SetChildIndex(this.panelTwo, 0);
          this.shownSlide = this.preparedSlideTwo;
          this.shownContainer = ShownContainer.Two;
          break;
        case ShownContainer.None:
        case ShownContainer.Two:
          this.Controls.SetChildIndex(this.panelOne, 0);
          this.shownSlide = this.preparedSlideOne;
          this.shownContainer = ShownContainer.One;
          break;
      }

      // Reset response fields
      this.currentMousebutton = MouseButtons.None;
      this.currentKey = Keys.None;

      // Need a refresh here because otherwise the video
      // capture will be started before the screen has been redrawn
      // and it would start with a frame of the old
      // slide.
      this.Refresh();

      if (this.shownSlide.Timer.Period > 1)
      {
        this.shownSlide.Timer.Start();
      }
    }

    /// <summary>
    /// This method updates the slide container that is 
    /// the next to be displayed with a new slide.
    /// </summary>
    /// <param name="trialCounter">The zero-based index of the trial
    /// in the trials list.</param>
    /// <param name="slideCounter">The zero-based index of the slide
    /// in the trials slide list.</param>
    private void PrepareSpecificSlide(int trialCounter, int slideCounter)
    {
      switch (this.shownContainer)
      {
        case ShownContainer.One:
          this.DisposeSlideContainer(this.preparedSlideTwo);
          this.preparedSlideTwo.Trial = this.trials[trialCounter];
          this.preparedSlideTwo.Slide = this.preparedSlideTwo.Trial[slideCounter];
          this.InitializeNextSlide(this.preparedSlideTwo);
          this.DrawToBuffer(this.preparedSlideTwo);
          break;
        case ShownContainer.Two:
          this.DisposeSlideContainer(this.preparedSlideOne);
          this.preparedSlideOne.Trial = this.trials[trialCounter];
          this.preparedSlideOne.Slide = this.preparedSlideOne.Trial[slideCounter];
          this.InitializeNextSlide(this.preparedSlideOne);
          this.DrawToBuffer(this.preparedSlideOne);
          break;
      }
    }

    /// <summary>
    /// This method is called whenever the slide has been swapped to the 
    /// foreground and its replay of audio streams and 
    /// optional screen capturing should be started.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// that should be initiated for replay.</param>
    private void PlaySlideContainer(SlidePresentationContainer slideContainer)
    {
      if (slideContainer.AudioPlayer.PlayState != OgamaControls.PlayState.Running)
      {
        slideContainer.AudioPlayer.Play();
      }

      if (this.shownSlide.Slide.HasFlashContent)
      {
        foreach (VGFlash flash in slideContainer.Slide.ActiveXStimuli)
        {
          flash.SendMessagesToParent(true);
        }

        switch (this.shownContainer)
        {
          case ShownContainer.One:
            if (this.preparedSlideOne.ScreenCapture != null)// && this.preparedSlideOne.ScreenCapture.Cued)
            {
              this.preparedSlideOne.ScreenCapture.PreviewWindow = this.screenCapturePreviewWindow;
              this.preparedSlideOne.ScreenCapture.Start();
            }

            break;
          case ShownContainer.Two:
            if (this.preparedSlideTwo.ScreenCapture != null)// && this.preparedSlideTwo.ScreenCapture.Cued)
            {
              this.preparedSlideTwo.ScreenCapture.PreviewWindow = this.screenCapturePreviewWindow;
              this.preparedSlideTwo.ScreenCapture.Start();
            }

            break;
        }
      }
    }

    /// <summary>
    /// This method setups the mouse cursor of the new slide.
    /// Its sets the position and visibility of the cursor.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// that should be initialized.</param>
    private void SetupMouse(SlidePresentationContainer slideContainer)
    {
      // Show or hide mouse cursor at specified position.
      if (slideContainer.Slide.MouseCursorVisible)
      {
        // Reset the cursor position to initial location if applicable
        if (slideContainer.Slide.ForceMousePositionChange)
        {
          Point newPoint = new Point(
            slideContainer.Slide.MouseInitialPosition.X + this.presentationBounds.Left,
            slideContainer.Slide.MouseInitialPosition.Y + this.presentationBounds.Top);
          Cursor.Position = newPoint;
        }

        if (this.hiddenCursor)
        {
          Cursor.Show();
          this.hiddenCursor = false;
        }
      }
      else
      {
        if (!this.hiddenCursor)
        {
          Cursor.Hide();
          this.hiddenCursor = true;
        }
      }
    }

    /// <summary>
    /// This method checks the <see cref="Slide.Links"/> collection,
    /// if the given response is in it, if so it sets the newTrialID
    /// ouput parameter, otherwise it will be -1.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/> for which to check the links for.</param>
    /// <param name="response">A <see cref="StopCondition"/> with the current response.</param>
    /// <param name="isLink">Out. <strong>True</strong> if this response is a link, otherwise <strong>false</strong>.</param>
    /// <param name="newTrialID">Out. An <see cref="Int32"/> with the new trial ID to link to.</param>
    private void CheckLinks(SlidePresentationContainer slideContainer, StopCondition response, out bool isLink, out int newTrialID)
    {
      isLink = false;
      newTrialID = -1;
      foreach (StopCondition condition in slideContainer.Slide.Links)
      {
        if (condition is MouseStopCondition && response is MouseStopCondition)
        {
          MouseStopCondition linkMsc = (MouseStopCondition)condition;
          MouseStopCondition responseMsc = (MouseStopCondition)response;
          if (linkMsc.StopMouseButton == responseMsc.StopMouseButton &&
            linkMsc.Target == responseMsc.Target)
          {
            newTrialID = linkMsc.TrialID.Value;
            isLink = true;
            break;
          }
        }
        else if (condition is KeyStopCondition && response is KeyStopCondition)
        {
          KeyStopCondition linkKsc = (KeyStopCondition)condition;
          KeyStopCondition responseKsc = (KeyStopCondition)response;
          if (linkKsc.StopKey == responseKsc.StopKey)
          {
            newTrialID = linkKsc.TrialID.Value;
            isLink = true;
            break;
          }
        }
      }
    }

    /// <summary>
    /// This method steps through each of the stop conditions of the current slide.
    /// If any of them matches the current state, check for
    /// response correctness and set bChangeStimulus=true;
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/> for which the slide should be parsed.</param>
    /// <param name="changeSlide">Out. <strong>True</strong> if new slide should be shown.</param>
    /// <param name="response">Out. The <see cref="StopCondition"/> that ended the slide.</param>
    private void CheckResponses(SlidePresentationContainer slideContainer, out bool changeSlide, out StopCondition response)
    {
      changeSlide = false;
      response = null;

      foreach (StopCondition condition in slideContainer.Slide.StopConditions)
      {
        if (condition is MouseStopCondition)
        {
          MouseStopCondition msc = (MouseStopCondition)condition;
          if ((msc.CanBeAnyInputOfThisType && this.currentMousebutton != MouseButtons.None)
            || (this.currentMousebutton == msc.StopMouseButton))
          {
            foreach (VGElement shape in slideContainer.Slide.TargetShapes)
            {
              if (shape.Contains(this.PointToClient(Control.MousePosition)))
              {
                response = new MouseStopCondition(msc.StopMouseButton, false, shape.Name, null, Control.MousePosition);
                if (msc.Target != string.Empty && (shape.Name == msc.Target || msc.Target == "Any"))
                {
                  changeSlide = true;
                }

                break;
              }
            }

            if (msc.Target == string.Empty)
            {
              changeSlide = true;
              if (response == null)
              {
                response = new MouseStopCondition(msc.StopMouseButton, false, string.Empty, null, Control.MousePosition);
              }
            }

            if (changeSlide)
            {
              // Check testing condition if specified.
              foreach (StopCondition correctCondition in slideContainer.Slide.CorrectResponses)
              {
                if (msc.Equals(correctCondition))
                {
                  response.IsCorrectResponse = true;
                  break;
                }
                else
                {
                  response.IsCorrectResponse = false;
                }
              }

              this.currentMousebutton = MouseButtons.None;
            }
          }
        }
        else if (condition is KeyStopCondition)
        {
          KeyStopCondition ksc = (KeyStopCondition)condition;
          if ((ksc.CanBeAnyInputOfThisType && this.currentKey != Keys.None)
            || (this.currentKey == ksc.StopKey))
          {
            changeSlide = true;
            response = new KeyStopCondition(ksc.StopKey, false, null);

            // Check testing condition if specified.
            if (slideContainer.Slide.CorrectResponses != null)
            {
              // Check testing condition if specified.
              foreach (StopCondition correctCondition in slideContainer.Slide.CorrectResponses)
              {
                if (ksc.Equals(correctCondition))
                {
                  response.IsCorrectResponse = true;
                  break;
                }
                else
                {
                  response.IsCorrectResponse = false;
                }
              }
            }

            this.currentKey = Keys.None;
            break;
          }
        }
      }
    }

    /// <summary>
    /// This method scans the <see cref="SlidePresentationContainer.ElementsWithAudioOnClick"/>
    /// for an element that was clicked and plays it if it was the case.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/> to search for audio files.</param>
    /// <param name="point">A <see cref="Point"/> with the click location.</param>
    /// <param name="eventTime">A <see cref="Int64"/> with the timestamp of the mouse click.</param>
    private void CheckforAudioStimulusOnClick(SlidePresentationContainer slideContainer, Point point, long eventTime)
    {
      foreach (VGElement element in slideContainer.ElementsWithAudioOnClick)
      {
        if (element.Contains(point))
        {
          slideContainer.AudioPlayer.AddAudioChannel(element.Sound.FullFilename);
          if (slideContainer.AudioPlayer.PlayState != OgamaControls.PlayState.Running)
          {
            slideContainer.AudioPlayer.Play();
          }

          MediaEvent soundEvent = new MediaEvent();
          soundEvent.Type = EventType.Audio;
          soundEvent.Task = MediaEventTask.Start;
          soundEvent.Param = Path.GetFileName(element.Sound.Filename);
          this.OnTrialEventOccured(new TrialEventOccuredEventArgs(soundEvent, eventTime));

          break;
        }
      }
    }

    /// <summary>
    /// This method creates the initialization trial.
    /// </summary>
    private void InitializeFirstTrial()
    {
      StopConditionCollection coll = new StopConditionCollection();
      TimeStopCondition stc = new TimeStopCondition(5000);
      coll.Add(stc);

      this.preparedSlideOne.Slide = new Slide(
        "OgamaDummyStartTrial6gsj2",
        Color.Gray,
        Images.CreateRecordInstructionImage(Document.ActiveDocument.ExperimentSettings.WidthStimulusScreen, Document.ActiveDocument.ExperimentSettings.HeightStimulusScreen),
        coll,
        new StopConditionCollection(),
        string.Empty,
        Document.ActiveDocument.PresentationSize);
      VGText wait = new VGText(
        ShapeDrawAction.None,
        "Initializing ...",
        new Font("Verdana", 40f),
        Color.WhiteSmoke,
        HorizontalAlignment.Center,
        1,
        6,
        Pens.Red,
        Brushes.Red,
        SystemFonts.MenuFont,
        Color.Black,
        new RectangleF(100, 100, 400, 200),
        VGStyleGroup.None,
        "Text",
        string.Empty,
        null);
      this.preparedSlideOne.Slide.VGStimuli.Add(wait);

      // Reset the cursor position to initial location if applicable
      Point newPoint = new Point(
        this.presentationBounds.Left + this.presentationBounds.Width / 2,
        this.presentationBounds.Top + this.presentationBounds.Height / 2);
      Cursor.Position = newPoint;

      Cursor.Hide();
      this.hiddenCursor = true;

      // Prepare preparation slide 
      this.preparedSlideOne.Trial = new Trial("DummyTrial", -1);
      this.preparedSlideOne.Trial.Add(this.preparedSlideOne.Slide);
      this.preparedSlideOne.Timer.Period = 2000;
      this.preparedSlideOne.Timer.Mode = TimerMode.OneShot;
      this.preparedSlideOne.Timer.SynchronizingObject = this;
      this.preparedSlideOne.Timer.Tick += new EventHandler(this.timer_Tick);

      this.DrawToBuffer(this.preparedSlideOne);
      this.PresentPreparedSlide();

      // Prepare first slide of trial list
      this.preparedSlideTwo.Trial = this.trials[0];
      this.preparedSlideTwo.Slide = this.trials[0][0];
      this.preparedSlideTwo.Timer.Period = 200;
      this.preparedSlideTwo.Timer.Mode = TimerMode.OneShot;
      this.preparedSlideTwo.Timer.SynchronizingObject = this;
      this.preparedSlideTwo.Timer.Tick += new EventHandler(this.timer_Tick);
      this.InitializeNextSlide(this.preparedSlideTwo);
      this.DrawToBuffer(this.preparedSlideTwo);
      this.PrepareScreenCapture(-1);
    }

    /// <summary>
    /// This method initializes the next slide in the given
    /// <see cref="SlidePresentationContainer"/>.
    /// That is setting a time stop condition timer,
    /// setup audio replay, and load flash objects.
    /// Because this can last a significant amount of time
    /// it should be done in a background thread.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// that should be initialized.</param>
    private void InitializeNextSlide(SlidePresentationContainer slideContainer)
    {
      try
      {
        // Reset Timer to 1ms period indicating that it should not be used during
        // call to PlaySlideContainer
        slideContainer.Timer.Period = 1;

        // Reset the timer according to stop condition if applicable.
        foreach (StopCondition condition in slideContainer.Slide.StopConditions)
        {
          if (condition is TimeStopCondition)
          {
            TimeStopCondition timeCondition = (TimeStopCondition)condition;
            slideContainer.Timer.Period = timeCondition.Duration;
            break;
          }
        }

        // Search for audio files
        this.ParseElementsForAudio(slideContainer);

        // Load background sound
        if (slideContainer.Slide.BackgroundSound != null)
        {
          if (slideContainer.Slide.BackgroundSound.ShouldPlay)
          {
            slideContainer.AudioPlayer.AddAudioChannel(slideContainer.Slide.BackgroundSound.FullFilename);
          }
        }

        // Check for flash stimuli and load them into the
        // flashObject activeX control.
        foreach (VGFlash flash in slideContainer.Slide.ActiveXStimuli)
        {
          flash.InitializeOnControl(slideContainer.ContainerControl, true, new System.Drawing.Drawing2D.Matrix());
        }
      }
      catch (Exception ex)
      {
        bool rethrow = ExceptionPolicy.HandleException(ex, "Global Policy");
        if (rethrow)
        {
          throw;
        }
      }
    }

    /// <summary>
    /// This method releases the resources used in the given
    /// <see cref="SlidePresentationContainer"/> but not all.
    /// Some items should not be disposed because they are reused for
    /// the next slide.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/>
    /// to be disposed and prepared for next use.</param>
    private void DisposeSlideContainer(SlidePresentationContainer slideContainer)
    {
      // Explicitely dispose flash objects 
      // otherwise we will get an exception from 
      // the MDA reportAvOnComRelease
      if (slideContainer.ContainerControl.Controls.Count > 0)
      {
        foreach (Control ctrl in slideContainer.ContainerControl.Controls)
        {
          if (ctrl is AxFlashControl)
          {
            if (ctrl.InvokeRequired)
            {
              MethodInvoker ctrlDisposeDelegate = new MethodInvoker(ctrl.Dispose);
              ctrl.Invoke(ctrlDisposeDelegate);
            }
            else
            {
              ctrl.Dispose();
            }
          }
        }
      }

      foreach (VGFlash flash in slideContainer.Slide.ActiveXStimuli)
      {
        flash.SendMessagesToParent(false);
      }

      if (this.InvokeRequired)
      {
        MethodInvoker controlsClearMethod = new MethodInvoker(slideContainer.ContainerControl.Controls.Clear);
        this.Invoke(controlsClearMethod);
      }
      else
      {
        slideContainer.ContainerControl.Controls.Clear();
      }

      slideContainer.Slide.Dispose();

      // Stop audio playback and release player
      slideContainer.AudioPlayer.CloseAudioFile();
      slideContainer.ElementsWithAudioOnClick.Clear();

      // Stop current running timer.
      if (slideContainer.Timer.IsRunning)
      {
        slideContainer.Timer.Stop();
      }
    }

    /// <summary>
    /// This method sends trigger signals to the ports if 
    /// triggering is enabled in the module.
    /// First it sends the general trigger, and the the slide trigger, if
    /// it is enabled.
    /// </summary>
    /// <param name="slideContainer">The <see cref="SlidePresentationContainer"/> that should send the slide trigger.</param>
    /// <remarks>This method is called in a separate thread, because
    /// the signaling time could be long lasting.</remarks>
    private void SendTrigger(SlidePresentationContainer slideContainer)
    {
      try
      {
        if (this.enableTrigger)
        {
          // Send general trigger if applicable
          if (this.generalTrigger.Signaling != TriggerSignaling.None)
          {
            this.generalTrigger.Send();
          }

          // Send Slide trigger if applicable
          if (this.generalTrigger.Signaling != TriggerSignaling.Override)
          {
            if (slideContainer.Slide.TriggerSignal.Signaling == TriggerSignaling.Enabled)
            {
              slideContainer.Slide.TriggerSignal.Send();
            }
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }

    #endregion //METHODS

    ///////////////////////////////////////////////////////////////////////////////
    // Small helping Methods                                                     //
    ///////////////////////////////////////////////////////////////////////////////
    #region HELPER

    /// <summary>
    /// This method initializes the user canera on first start of the
    /// presentation.
    /// </summary>
    /// <param name="value">A <see cref="CaptureDeviceProperties"/>
    /// with the device and compressor to be used for the webcam.</param>
    private void InitializeUserCamera(CaptureDeviceProperties value)
    {
      if (value == null)
      {
        // No need to initalize
        return;
      }

      this.userCamera = new Webcam(this.userCameraPreviewWindow);
      this.userCamera.Properties = value;

      // Rebuild graph
      this.userCamera.Preview = true;
    }

    #endregion //HELPER
  }
}
