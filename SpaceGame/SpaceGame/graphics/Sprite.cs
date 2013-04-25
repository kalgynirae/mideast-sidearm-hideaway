﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceGame.graphics
{
    class Sprite
    {
        #region constant
        const string SPRITE_FOLDER_PATH = "spritesheets/";
        #endregion
        
        #region static
        public static ContentManager Content;
        static Rectangle tempRect;   //temporary rectangle for cross instance use
        #endregion

        #region fields
        //data to initialize sprites. Initialize this in Game.Initialize
        public static Dictionary<string, SpriteData> Data;
        //length of each animation, in # of frames
        int _framesPerAnimation;
        //number of different animations, e.g. for facing different directions
        int _numStates;
        //time between each animation frame, and countdown to next frame
        TimeSpan _animationInterval, _timeTillNext;
        //used if PlayAnimation is called
        public bool Animating { get; private set;}
        public bool AnimationOver { get; private set;}
        //current frame of animation
        int _currentFrame = 0;
        //current animation state
        int _currentState = 0;
        //width and height of sprite
        int _frameWidth, _frameHeight;
        //spritesheet of animation frames
        //(_framesPerAnimation) # of sprites high, (_numStates) # of sprites wide
        //height(pixels) = _framesPerAnimation * _frameHeight, width(pixels) = _numStates * _frameWidth
        Texture2D _spriteSheet;
        //scale and rotation
        float _defaultScale;
        float _scale;
        float _angle = 0.0f;
        //layer on which to draw sprite
        private float _zLayer;
        //size of sprite
        Rectangle _size;
        //shade do draw the sprite with
        private Color _shade;
        //2Darray of rects to select from sprite sheet
        Rectangle[,] _rects;
        //origin used for rotation
        Vector2 _textureCenter;

        //for Flash(Color, Time)
        Color _flashColor;
        int _flashCounter = 0;
        TimeSpan _currentFlashTime, _halfFlashTime;
        #endregion

        #region properties
        public Color Shade
        {
            get { return _shade; }
            set
            {
                _flashCounter = 0;  //stop flashing if shade manually set
                _shade = value;
            }
        }

        public float Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                //Recalculate the Size of the Sprite with the new scale
                _size = new Rectangle(0, 0, (int)(_frameWidth * Scale), (int)(_frameHeight * Scale));
            }
        }

        public Vector2 TextureCenter
        {
            get { return _textureCenter; }
        }

        //scale relative to default scale
        public float ScaleFactor
        {
            get {return _scale / _defaultScale;}
            set { Scale = value * _defaultScale; }
        }

        public float Angle
        {
            get { return _angle; }
            set { _angle = value; }
        }

        public int AnimationState
        {
            get { return _currentState; }
            set { _currentState = value % _numStates; }
        }

        public Vector2 Center {get {return new Vector2(_size.Center.X, _size.Center.Y);}}
        public float Height { get { return _size.Bottom; } }
        public float Width { get { return _size.Right; } }
        public float FrameHeight { get { return _frameHeight; } }
        public float FrameWidth { get { return _frameWidth; } }
        public TimeSpan FullAnimationTime
        {
            get 
            {
                return TimeSpan.FromSeconds(
                (float)_animationInterval.TotalSeconds * _framesPerAnimation);
            }
        }
        public bool FlipH { get; set; }
        public bool FlipV { get; set; }
        #endregion properties

        #region methods
        /// <summary>
        /// Create a new animated sprite
        /// </summary>
        /// <param name="spriteName">key used to find sprite data in SpriteData Dictionary</param>
        public Sprite(string spriteName)
        {
            SpriteData spriteData = Data[spriteName];
            _frameWidth = spriteData.FrameWidth; _frameHeight = spriteData.FrameHeight;
            _textureCenter = new Vector2(_frameWidth / 2.0f, _frameHeight / 2.0f);
            _framesPerAnimation = spriteData.NumFrames;
            _numStates = spriteData.NumStates;
            _defaultScale = spriteData.DefaultScale;
            ScaleFactor = 1.0f;      //use property to set rect
            _animationInterval = TimeSpan.FromSeconds(spriteData.SecondsPerAnimation);
            _timeTillNext = _animationInterval;
            initRects();
            Shade = Color.White;
            _zLayer = spriteData.ZLayer;
            _spriteSheet = Content.Load<Texture2D>(SPRITE_FOLDER_PATH + spriteData.AssetName);
        }

        private void initRects()
        {
            _rects = new Rectangle[_numStates, _framesPerAnimation];

            for (int y = 0; y < _framesPerAnimation; y++)
            {
                for (int x = 0; x < _numStates; x++)
                {
                    _rects[x, y] = new Rectangle(x * _frameWidth, y * _frameHeight, _frameWidth, _frameHeight);
                }
            }
        }

        public void Reset()
        {
            _timeTillNext = _animationInterval;
            _currentState = 0; _currentFrame = 0;
            Shade = Color.White;
            _angle = 0.0f;
            Scale = _defaultScale;
        }

        public void Update(GameTime theGameTime)
        {
            _timeTillNext -= theGameTime.ElapsedGameTime;

            if (Animating && _timeTillNext <= TimeSpan.Zero && _currentFrame == _framesPerAnimation - 1)
                AnimationOver = true;

            if (_timeTillNext < TimeSpan.Zero)
            {
                _timeTillNext = _animationInterval;
                _currentFrame = (_currentFrame + 1) % _framesPerAnimation;
            }

            if (_flashCounter > 0)
            {
                _currentFlashTime += theGameTime.ElapsedGameTime;

                if (_currentFlashTime < _halfFlashTime)
                {
                    _shade = Color.Lerp(Color.White, _flashColor, (float)_currentFlashTime.TotalSeconds / (float)_halfFlashTime.TotalSeconds);
                    _currentFlashTime += theGameTime.ElapsedGameTime;
                }
                else if (_currentFlashTime >= (_halfFlashTime + _halfFlashTime))
                {
                    _currentFlashTime = TimeSpan.Zero;
                    _flashCounter -= 1;
                    if (_flashCounter == 0)
                        _shade = Color.White;
                }
                else
                {
                    _shade = Color.Lerp(_flashColor, Color.White, (float)(_currentFlashTime - _halfFlashTime).TotalSeconds / (float)_halfFlashTime.TotalSeconds);
                }

            }

        }

        public void PlayAnimation(int animationNumber)
        {
            Animating = true;
            AnimationOver = false;
            AnimationState = animationNumber % _numStates;
            _currentFrame = 0;
        }

        public void Flash(Color color, TimeSpan timePerFlash, int numFlashes)
        {
            _flashColor = color;
            _flashCounter = numFlashes;
            _currentFlashTime = TimeSpan.Zero;
            _halfFlashTime = TimeSpan.FromSeconds(timePerFlash.TotalSeconds / 2);
        }

        public void Draw(SpriteBatch batch, Vector2 position)
        {
            Draw(batch, position, _angle);
        }

        public void Draw(SpriteBatch batch, Vector2 position, float rotation)
        {
            if (FlipH && FlipV)
                batch.Draw(_spriteSheet, position, _rects[_currentState, _currentFrame], Shade, rotation, _textureCenter, 
                    Scale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, _zLayer);
            else if (FlipV)
                batch.Draw(_spriteSheet, position, _rects[_currentState, _currentFrame], Shade, rotation, _textureCenter, 
                    Scale, SpriteEffects.FlipVertically, _zLayer);
            else if (FlipH)
                batch.Draw(_spriteSheet, position, _rects[_currentState, _currentFrame], Shade, rotation, _textureCenter, 
                    Scale, SpriteEffects.FlipHorizontally, _zLayer);
            else
                batch.Draw(_spriteSheet, position, _rects[_currentState, _currentFrame], Shade, rotation, _textureCenter, 
                    Scale, SpriteEffects.None, _zLayer);
        }

        public void DrawFragment(SpriteBatch batch, int row, int col, int numDivisions, Vector2 position, float angle)
        {
            tempRect = _rects[_currentState, _currentFrame];
            tempRect.X += tempRect.Width * col / numDivisions;
            tempRect.Y += tempRect.Height * row / numDivisions;
            tempRect.Width /= numDivisions;
            tempRect.Height /= numDivisions;
            batch.Draw(_spriteSheet, position, tempRect, Shade, angle, _textureCenter, Scale * 2, SpriteEffects.None, _zLayer);
        }

        #endregion

    }

}
