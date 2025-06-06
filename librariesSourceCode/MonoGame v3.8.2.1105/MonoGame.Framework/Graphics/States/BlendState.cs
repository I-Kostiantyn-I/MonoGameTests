// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Contains blend state for the device.
    /// </summary>
	public partial class BlendState : GraphicsResource
	{
        private readonly TargetBlendState[] _targetBlendState;

        private readonly bool _defaultStateObject;

	    private Color _blendFactor;

	    private int _multiSampleMask;

	    private bool _independentBlendEnable;

        internal void BindToGraphicsDevice(GraphicsDevice device)
        {
            if (_defaultStateObject)
                throw new InvalidOperationException("You cannot bind a default state object.");
            if (GraphicsDevice != null && GraphicsDevice != device)
                throw new InvalidOperationException("This blend state is already bound to a different graphics device.");
            GraphicsDevice = device;
        }

        internal void ThrowIfBound()
        {
            if (_defaultStateObject)
                throw new InvalidOperationException("You cannot modify a default blend state object.");
            if (GraphicsDevice != null)
                throw new InvalidOperationException("You cannot modify the blend state after it has been bound to the graphics device!");
        }

        /// <summary>
        /// Returns the target specific blend state.
        /// </summary>
        /// <param name="index">The 0 to 3 target blend state index.</param>
        /// <returns>A target blend state.</returns>
        public TargetBlendState this[int index]
        {
            get { return _targetBlendState[index]; }
        }

        /// <summary>
        /// Gets or sets the arithmetic operation when blending alpha values.
        /// The default is <see cref="BlendFunction.Add"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="BlendFunction"/> enumeration.
        /// </value>
	    public BlendFunction AlphaBlendFunction
	    {
	        get { return _targetBlendState[0].AlphaBlendFunction; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].AlphaBlendFunction = value;
            }
	    }

        /// <summary>
        /// Gets or sets the blend factor for the destination alpha, which is the
        /// percentage of the destination alpha included in the blended result.
        /// The default is <see cref="Blend.One"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="Blend"/> enumeration.
        /// </value>
		public Blend AlphaDestinationBlend
        {
            get { return _targetBlendState[0].AlphaDestinationBlend; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].AlphaDestinationBlend = value;
            }
        }

        /// <summary>
        /// Gets or sets the alpha blend factor.
        /// The default is <see cref="Blend.One"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="Blend"/> enumeration.
        /// </value>
		public Blend AlphaSourceBlend
        {
            get { return _targetBlendState[0].AlphaSourceBlend; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].AlphaSourceBlend = value;
            }
        }

        /// <summary>
        /// Gets or sets the arithmetic operation when blending color values.
        /// The default is <see cref="BlendFunction.Add"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="BlendFunction"/> enumeration.
        /// </value>
		public BlendFunction ColorBlendFunction
        {
            get { return _targetBlendState[0].ColorBlendFunction; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].ColorBlendFunction = value;
            }
        }

        /// <summary>
        /// Gets or sets the blend factor for the destination color.
        /// The default is <see cref="Blend.One"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="Blend"/> enumeration.
        /// </value>
		public Blend ColorDestinationBlend
        {
            get { return _targetBlendState[0].ColorDestinationBlend; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].ColorDestinationBlend = value;
            }
        }

        /// <summary>
        /// Gets or sets the blend factor for the source color.
        /// The default is <see cref="Blend.One"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="Blend"/> enumeration.
        /// </value>
		public Blend ColorSourceBlend
        {
            get { return _targetBlendState[0].ColorSourceBlend; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].ColorSourceBlend = value;
            }
        }

        /// <summary>
        /// Gets or sets which color channels (RGBA) are enabled for writing
        /// during color blending.
        /// The default value is <see cref="ColorWriteChannels.None"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="ColorWriteChannels"/> enumeration.
        /// </value>
		public ColorWriteChannels ColorWriteChannels
        {
            get { return _targetBlendState[0].ColorWriteChannels; }
            set
            {
                ThrowIfBound();
                _targetBlendState[0].ColorWriteChannels = value;
            }
        }

        /// <summary>
        /// Gets or sets which color channels (RGBA) are enabled for writing
        /// during color blending.
        /// The default value is <see cref="ColorWriteChannels.None"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="ColorWriteChannels"/> enumeration.
        /// </value>
		public ColorWriteChannels ColorWriteChannels1
        {
            get { return _targetBlendState[1].ColorWriteChannels; }
            set
            {
                ThrowIfBound();
                _targetBlendState[1].ColorWriteChannels = value;
            }
        }

        /// <summary>
        /// Gets or sets which color channels (RGBA) are enabled for writing
        /// during color blending.
        /// The default value is <see cref="ColorWriteChannels.None"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="ColorWriteChannels"/> enumeration.
        /// </value>
		public ColorWriteChannels ColorWriteChannels2
        {
            get { return _targetBlendState[2].ColorWriteChannels; }
            set
            {
                ThrowIfBound();
                _targetBlendState[2].ColorWriteChannels = value;
            }
        }

        /// <summary>
        /// Gets or sets which color channels (RGBA) are enabled for writing
        /// during color blending.
        /// The default value is <see cref="ColorWriteChannels.None"/>.
        /// </summary>
        /// <value>
        /// A value from the <see cref="ColorWriteChannels"/> enumeration.
        /// </value>
		public ColorWriteChannels ColorWriteChannels3
        {
            get { return _targetBlendState[3].ColorWriteChannels; }
            set
            {
                ThrowIfBound();
                _targetBlendState[3].ColorWriteChannels = value;
            }
        }

        /// <summary>
        /// The color used as blend factor when alpha blending.
        /// </summary>
        /// <remarks>
        /// <see cref="P:Microsoft.Xna.Framework.Graphics.GraphicsDevice.BlendFactor"/> is set to this value when this <see cref="BlendState"/>
        /// is bound to a GraphicsDevice.
        /// </remarks>
	    public Color BlendFactor
	    {
	        get { return _blendFactor; }
            set
            {
                ThrowIfBound();
                _blendFactor = value;
            }
	    }

        /// <summary>
        /// Gets or sets a bitmask which defines which samples can be written
        /// during multisampling. The default is <c>0xffffffff</c>.
        /// </summary>
        public int MultiSampleMask
        {
            get { return _multiSampleMask; }
            set
            {
                ThrowIfBound();
                _multiSampleMask = value;
            }
        }

        /// <summary>
        /// Enables use of the per-target blend states.
        /// </summary>
        public bool IndependentBlendEnable
        {
            get { return _independentBlendEnable; }
            set
            {
                ThrowIfBound();
                _independentBlendEnable = value;
            }
        }


        /// <summary>
        /// A built-in state object with settings for additive blend that is
        /// adding the destination data to the source data without using alpha.
        /// </summary>
        /// <remarks>
        /// This built-in state object has the following settings:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Property</term>
        ///         <description>Value</description>
        ///     </listheader>
        ///     <item>
        ///         <term>ColorSourceBlend</term>
        ///         <description><see cref="Blend.SourceAlpha"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaSourceBlend</term>
        ///         <description><see cref="Blend.SourceAlpha"/></description>
        ///     </item>
        ///     <item>
        ///         <term>ColorDestinationBlend</term>
        ///         <description><see cref="Blend.One"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaDestinationBlend</term>
        ///         <description><see cref="Blend.One"/></description>
        ///     </item>
        /// </list>
        /// </remarks>
        public static readonly BlendState Additive;

        /// <summary>
        /// A built-in state object with settings for alpha blend that is
        /// blending the source and destination data using alpha.
        /// </summary>
        /// <remarks>
        /// This built-in state object has the following settings:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Property</term>
        ///         <description>Value</description>
        ///     </listheader>
        ///     <item>
        ///         <term>ColorSourceBlend</term>
        ///         <description><see cref="Blend.One"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaSourceBlend</term>
        ///         <description><see cref="Blend.One"/></description>
        ///     </item>
        ///     <item>
        ///         <term>ColorDestinationBlend</term>
        ///         <description><see cref="Blend.InverseSourceAlpha"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaDestinationBlend</term>
        ///         <description><see cref="Blend.InverseSourceAlpha"/></description>
        ///     </item>
        /// </list>
        /// </remarks>
        public static readonly BlendState AlphaBlend;

        /// <summary>
        /// A built-in state object with settings for blending with non-premultipled
        /// alpha that is blending source and destination data by using alpha
        /// while assuming the color data contains no alpha information.
        /// </summary>
        /// <remarks>
        /// This built-in state object has the following settings:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Property</term>
        ///         <description>Value</description>
        ///     </listheader>
        ///     <item>
        ///         <term>ColorSourceBlend</term>
        ///         <description><see cref="Blend.SourceAlpha"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaSourceBlend</term>
        ///         <description><see cref="Blend.SourceAlpha"/></description>
        ///     </item>
        ///     <item>
        ///         <term>ColorDestinationBlend</term>
        ///         <description><see cref="Blend.InverseSourceAlpha"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaDestinationBlend</term>
        ///         <description><see cref="Blend.InverseSourceAlpha"/></description>
        ///     </item>
        /// </list>
        /// </remarks>
        public static readonly BlendState NonPremultiplied;

        /// <summary>
        /// A built-in state object with settings for opaque blend that is
        /// overwriting the source with the destination data.
        /// </summary>
        /// <remarks>
        /// This built-in state object has the following settings:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Property</term>
        ///         <description>Value</description>
        ///     </listheader>
        ///     <item>
        ///         <term>ColorSourceBlend</term>
        ///         <description><see cref="Blend.One"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaSourceBlend</term>
        ///         <description><see cref="Blend.One"/></description>
        ///     </item>
        ///     <item>
        ///         <term>ColorDestinationBlend</term>
        ///         <description><see cref="Blend.Zero"/></description>
        ///     </item>
        ///     <item>
        ///         <term>AlphaDestinationBlend</term>
        ///         <description><see cref="Blend.Zero"/></description>
        ///     </item>
        /// </list>
        /// </remarks>
        public static readonly BlendState Opaque;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlendState"/> class with
        /// the default values, using additive color and alpha blending.
        /// </summary>
        public BlendState()
        {
            _targetBlendState = new TargetBlendState[4];
            _targetBlendState[0] = new TargetBlendState(this);
            _targetBlendState[1] = new TargetBlendState(this);
            _targetBlendState[2] = new TargetBlendState(this);
            _targetBlendState[3] = new TargetBlendState(this);

			_blendFactor = Color.White;
            _multiSampleMask = Int32.MaxValue;
            _independentBlendEnable = false;
        }

        private BlendState(string name, Blend sourceBlend, Blend destinationBlend)
            : this()
        {
            Name = name;
            ColorSourceBlend = sourceBlend;
            AlphaSourceBlend = sourceBlend;
            ColorDestinationBlend = destinationBlend;
            AlphaDestinationBlend = destinationBlend;
            _defaultStateObject = true;
        }

        private BlendState(BlendState cloneSource)
        {
            Name = cloneSource.Name;

            _targetBlendState = new TargetBlendState[4];
            _targetBlendState[0] = cloneSource[0].Clone(this);
            _targetBlendState[1] = cloneSource[1].Clone(this);
            _targetBlendState[2] = cloneSource[2].Clone(this);
            _targetBlendState[3] = cloneSource[3].Clone(this);

            _blendFactor = cloneSource._blendFactor;
            _multiSampleMask = cloneSource._multiSampleMask;
            _independentBlendEnable = cloneSource._independentBlendEnable;
        }

        static BlendState()
        {
            Additive = new BlendState("BlendState.Additive", Blend.SourceAlpha, Blend.One);
            AlphaBlend = new BlendState("BlendState.AlphaBlend", Blend.One, Blend.InverseSourceAlpha);
            NonPremultiplied = new BlendState("BlendState.NonPremultiplied", Blend.SourceAlpha, Blend.InverseSourceAlpha);
            Opaque = new BlendState("BlendState.Opaque", Blend.One, Blend.Zero);
		}

	    internal BlendState Clone()
	    {
	        return new BlendState(this);
	    }

        partial void PlatformDispose();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                for (int i = 0; i < _targetBlendState.Length; ++i)
                    _targetBlendState[i] = null;

                PlatformDispose();
            }
            base.Dispose(disposing);
        }
    }
}

