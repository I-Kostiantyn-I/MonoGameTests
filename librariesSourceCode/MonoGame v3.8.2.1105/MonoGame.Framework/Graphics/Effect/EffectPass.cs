using System.Diagnostics;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Contains the rendering state for drawing with an effect. <para/>
    /// An effect can contain one or more passes.
    /// </summary>
    public class EffectPass
    {
        private readonly Effect _effect;

		private readonly Shader _pixelShader;
        private readonly Shader _vertexShader;

        private readonly BlendState _blendState;
        private readonly DepthStencilState _depthStencilState;
        private readonly RasterizerState _rasterizerState;

        /// <summary>
        /// Gets the name of this pass.
        /// </summary>
		public string Name { get; private set; }

        /// <summary>
        /// Gets the collection of <see cref="EffectAnnotation"/> objects for this <see cref="EffectPass"/>.
        /// </summary>
        public EffectAnnotationCollection Annotations { get; private set; }

        internal EffectPass(    Effect effect, 
                                string name,
                                Shader vertexShader, 
                                Shader pixelShader, 
                                BlendState blendState, 
                                DepthStencilState depthStencilState, 
                                RasterizerState rasterizerState,
                                EffectAnnotationCollection annotations )
        {
            Debug.Assert(effect != null, "Got a null effect!");
            Debug.Assert(annotations != null, "Got a null annotation collection!");

            _effect = effect;

            Name = name;

            _vertexShader = vertexShader;
            _pixelShader = pixelShader;

            _blendState = blendState;
            _depthStencilState = depthStencilState;
            _rasterizerState = rasterizerState;

            Annotations = annotations;
        }
        
        internal EffectPass(Effect effect, EffectPass cloneSource)
        {
            Debug.Assert(effect != null, "Got a null effect!");
            Debug.Assert(cloneSource != null, "Got a null cloneSource!");

            _effect = effect;

            // Share all the immutable types.
            Name = cloneSource.Name;
            _blendState = cloneSource._blendState;
            _depthStencilState = cloneSource._depthStencilState;
            _rasterizerState = cloneSource._rasterizerState;
            Annotations = cloneSource.Annotations;
            _vertexShader = cloneSource._vertexShader;
            _pixelShader = cloneSource._pixelShader;
        }

        /// <summary>
        /// Begins this pass.
        /// </summary>
        public void Apply()
        {
            // Set/get the correct shader handle/cleanups.

            var current = _effect.CurrentTechnique;
            _effect.OnApply();
            if (_effect.CurrentTechnique != current)
            {
                _effect.CurrentTechnique.Passes[0].Apply();
                return;
            }

            var device = _effect.GraphicsDevice;

            if (_vertexShader != null)
            {
                device.VertexShader = _vertexShader;

				// Update the texture parameters.
                SetShaderSamplers(_vertexShader, device.VertexTextures, device.VertexSamplerStates);

                // Update the constant buffers.
                for (var c = 0; c < _vertexShader.CBuffers.Length; c++)
                {
                    var cb = _effect.ConstantBuffers[_vertexShader.CBuffers[c]];
                    cb.Update(_effect.Parameters);
                    device.SetConstantBuffer(ShaderStage.Vertex, c, cb);
                }
            }

            if (_pixelShader != null)
            {
                device.PixelShader = _pixelShader;

                // Update the texture parameters.
                SetShaderSamplers(_pixelShader, device.Textures, device.SamplerStates);
                
                // Update the constant buffers.
                for (var c = 0; c < _pixelShader.CBuffers.Length; c++)
                {
                    var cb = _effect.ConstantBuffers[_pixelShader.CBuffers[c]];
                    cb.Update(_effect.Parameters);
                    device.SetConstantBuffer(ShaderStage.Pixel, c, cb);
                }
            }

            // Set the render states if we have some.
            if (_rasterizerState != null)
                device.RasterizerState = _rasterizerState;
            if (_blendState != null)
                device.BlendState = _blendState;
            if (_depthStencilState != null)
                device.DepthStencilState = _depthStencilState;
        }

        private void SetShaderSamplers(Shader shader, TextureCollection textures, SamplerStateCollection samplerStates)
        {
            foreach (var sampler in shader.Samplers)
            {
                var param = _effect.Parameters[sampler.parameter];
                var texture = param.Data as Texture;

                textures[sampler.textureSlot] = texture;

                // If there is a sampler state set it.
                if (sampler.state != null)
                    samplerStates[sampler.samplerSlot] = sampler.state;
            }
        }
    }
}