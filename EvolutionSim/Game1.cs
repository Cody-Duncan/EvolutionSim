using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace EvolutionSim
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        Texture2D randomTexture;    //used to store a series of random values, that will be accessed from various shaders

        RenderTarget2D organismViewRT;  // render target that will hold the positions of the particles
        RenderTarget2D temporaryRT;     // temporary render target, needed when updating the other render targets

        Effect evolutionEffect;        // effect file used to update the physics (position and velocities)
        SpriteBatch spriteBatch;       // sprite batch used for 2D drawing
        SpriteFont font;

        IInputHandler inputHandler;

        int desiredScreenWidth = 1280;
        int desiredScreenHeight = 720;
        int screenWidth = 0;
        int screenHeight = 0;

        int generationCount;

        int rootOrganismCount = 256;            // number of organisms = rootOrganismCount ^ 2
        bool resetFlag = false;                 // if false, reset the organisms to random values for one generation
        bool newMigrationFlag = false;          // if true, add new organisms to the mix for one generation.
        bool randomMatingFlag = true;           // if true, randomly mate organisms.
        bool disasterFlag = false;
        bool newMutationFlag = false;
        bool newSelectionFlag = false;

        Vector2 toCenterRender = Vector2.Zero;

        PercentCatcher mutationCatcher;
        PercentCatcher natSelectCatcher;

        public Game1()
        {

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            //graphics.IsFullScreen = true;

            InputHandler tempHandler = new InputHandler(this);
            inputHandler = tempHandler;
            Components.Add(tempHandler);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            InitGraphicsMode(desiredScreenWidth, desiredScreenHeight, false);
            base.Initialize();
        }

        /// <summary>
        /// Attempt to set the display mode to the desired resolution.  Iterates through the display
        /// capabilities of the default graphics adapter to determine if the graphics adapter supports the
        /// requested resolution.  If so, the resolution is set and the function returns true.  If not,
        /// no change is made and the function returns false.
        /// </summary>
        /// <param name="iWidth">Desired screen width.</param>
        /// <param name="iHeight">Desired screen height.</param>
        /// <param name="bFullScreen">True if you wish to go to Full Screen, false for Windowed Mode.</param>
        private bool InitGraphicsMode(int iWidth, int iHeight, bool bFullScreen)
        {
            // If we aren't using a full screen mode, the height and width of the window can
            // be set to anything equal to or smaller than the actual screen size.
            if (bFullScreen == false)
            {
                if ((iWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                    && (iHeight <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height))
                {
                    graphics.PreferredBackBufferWidth = iWidth;
                    graphics.PreferredBackBufferHeight = iHeight;
                    graphics.IsFullScreen = bFullScreen;
                    graphics.ApplyChanges();

                    screenWidth = iWidth;
                    screenHeight = iHeight;
                    return true;
                }
            }
            else
            {
                // If we are using full screen mode, we should check to make sure that the display
                // adapter can handle the video mode we are trying to set.  To do this, we will
                // iterate thorugh the display modes supported by the adapter and check them against
                // the mode we want to set.
                foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check the width and height of each mode against the passed values
                    if ((dm.Width == iWidth) && (dm.Height == iHeight))
                    {
                        // The mode is supported, so set the buffer formats, apply changes and return
                        graphics.PreferredBackBufferWidth = iWidth;
                        graphics.PreferredBackBufferHeight = iHeight;
                        graphics.IsFullScreen = bFullScreen;
                        graphics.ApplyChanges();

                        screenWidth = iWidth;
                        screenHeight = iHeight;
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //load shaders
            evolutionEffect = Content.Load<Effect>("Evolution");
            evolutionEffect.Parameters["organismTexWidth"].SetValue(rootOrganismCount);
            evolutionEffect.Parameters["PixOffset"].SetValue( (1.0f / (float)rootOrganismCount) );

            Viewport viewport = GraphicsDevice.Viewport;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, rootOrganismCount, rootOrganismCount, 0, 0, 1);
            evolutionEffect.Parameters["MatrixTransform"].SetValue(projection);

            toCenterRender.X = (viewport.Width - rootOrganismCount) / 2;
            toCenterRender.Y = (viewport.Height - rootOrganismCount) / 2;

            //initialize renderTargets
            temporaryRT = new RenderTarget2D(graphics.GraphicsDevice, rootOrganismCount, rootOrganismCount, false, SurfaceFormat.Rgba64, DepthFormat.None);
            organismViewRT = new RenderTarget2D(graphics.GraphicsDevice, rootOrganismCount, rootOrganismCount, false, SurfaceFormat.Rgba64, DepthFormat.None);

            //generate a random texture for initial particle locations and colors
            randomTexture = new Texture2D(graphics.GraphicsDevice, rootOrganismCount, rootOrganismCount, true, SurfaceFormat.Rgba64);
            generateRandTexture();
            evolutionEffect.Parameters["randomMap"].SetValue(randomTexture);


            font = Content.Load<SpriteFont>("Consolas");
            mutationCatcher = new PercentCatcher(inputHandler, Keys.M);
            natSelectCatcher = new PercentCatcher(inputHandler, Keys.N);
        }

        private void generateRandTexture()
        {
            Random rand = new Random();

            //generate a random texture for initial particle locations and colors
            Rgba64[] pointsarray = new Rgba64[rootOrganismCount * rootOrganismCount];
            for (int i = 0; i < rootOrganismCount * rootOrganismCount; i++)
            {
                Vector4 temp = new Vector4(
                    (float)rand.NextDouble(),   //R
                    (float)rand.NextDouble(),   //G
                    (float)rand.NextDouble(),   //B
                    (float)rand.NextDouble()    //A
                );
                pointsarray[i] = new Rgba64(temp);
            }
            randomTexture.SetData<Rgba64>(pointsarray);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        Random rand = new Random();
        double totalMilliseconds = 0;
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            checkMouseInput();
            checkKeyboardInput();

            totalMilliseconds += gameTime.ElapsedGameTime.TotalMilliseconds;

            base.Update(gameTime);
        }

        private void checkMouseInput()
        {
            //get relative location  of mouse on window
            double xRelativeLocation = (double)inputHandler.MouseState.X / graphics.PreferredBackBufferWidth;
            double yRelativeLocation = (double)inputHandler.MouseState.Y / graphics.PreferredBackBufferHeight;

        }


        private void checkKeyboardInput()
        {
            if (mutationCatcher.update())
                newMutationFlag = true;
            if (mutationCatcher.active)
                return;

            if (natSelectCatcher.update())
                newSelectionFlag = true;
            if (natSelectCatcher.active)
                return;

            //enter = reset 
            if (inputHandler.KeyPressed(Keys.Enter) )
            {
                resetFlag = false;
            }

            if (inputHandler.KeyPressed(Keys.D1) || inputHandler.KeyHold(Keys.D1))
            {
                newMigrationFlag = true;
            }
            if (inputHandler.KeyPressed(Keys.D2))
            {
                randomMatingFlag = !randomMatingFlag;
            }
            if (inputHandler.KeyPressed(Keys.D3))
            {
                disasterFlag = true;
            }

        }


        private void SimulateGeneration(GameTime gameTime)
        {
            generateRandTexture();
            evolutionEffect.Parameters["randomMap"].SetValue(randomTexture);
            evolutionEffect.Parameters["elapsedTime"].SetValue((float)gameTime.ElapsedGameTime.TotalSeconds);
            evolutionEffect.Parameters["randomMating"].SetValue(randomMatingFlag);

            if (newMutationFlag)
            {
                evolutionEffect.Parameters["mutationChance"].SetValue(mutationCatcher.currentPercent);
                newMutationFlag = false;
            }

            if (newSelectionFlag)
            {
                Vector4 selectionVec = Vector4.Zero;
                if (natSelectCatcher.numList.Count >= 4)
                {
                    selectionVec = new Vector4(
                                    natSelectCatcher.numList[0],
                                    natSelectCatcher.numList[1],
                                    natSelectCatcher.numList[2],
                                    natSelectCatcher.numList[3]);
                }

                evolutionEffect.Parameters["selectionColor"].SetValue(selectionVec);
                newSelectionFlag = false;
            }

            if (!resetFlag)
            {
                RunTextureProcessing("ResetOrganisms", organismViewRT);
                resetFlag = true;
                generationCount = 0;
                totalMilliseconds = 0;
                Console.WriteLine("RESET");
            }

            RunTextureProcessing("UpdateOrganisms", organismViewRT);
            generationCount++;

            if (newMigrationFlag)
            {
                RunTextureProcessing("NewMigration", organismViewRT);
                newMigrationFlag = false;
            }
            if (disasterFlag)
            {
                RunTextureProcessing("Disaster", organismViewRT);
                disasterFlag = false;
            }
        }

        /// <summary>
        /// Uses the ParticlePhysics.fx shader to do physics calculations for velocity and position
        /// on the gpu.
        /// </summary>
        /// <param name="technique">the shader technique to apply</param>
        /// <param name="resultTarget">the rendertarget to copy the resulting data to</param>
        private void RunTextureProcessing(string technique, RenderTarget2D resultTarget)
        {
            //store old rendertarget
            RenderTarget2D oldRT = 
                graphics.GraphicsDevice.GetRenderTargets().Length == 1?
                graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D :
                null;

            //set render targets, clear, and choose technique
            graphics.GraphicsDevice.SetRenderTarget(temporaryRT);
            graphics.GraphicsDevice.Clear(Color.Black);
            evolutionEffect.CurrentTechnique = evolutionEffect.Techniques[technique];

            if (resetFlag) //set if not already set
            {
                evolutionEffect.Parameters["organismMap"].SetValue(organismViewRT);
            }

            //first operation
            //perform the shader operations
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
                evolutionEffect.CurrentTechnique.Passes[0].Apply();
                spriteBatch.Draw(randomTexture, new Rectangle(0, 0, rootOrganismCount, rootOrganismCount),Color.White); //must draw something to get shader to go
            spriteBatch.End();


            //second operation to copy back to rendertarget
            //set render target
            graphics.GraphicsDevice.SetRenderTarget(resultTarget);

            //set effect parameters
            evolutionEffect.Parameters["temporaryMap"].SetValue(temporaryRT);
            evolutionEffect.CurrentTechnique = evolutionEffect.Techniques["CopyTexture"];

            //draw with effect onto rendertarget
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                evolutionEffect.CurrentTechnique.Passes[0].Apply();
                spriteBatch.Draw(temporaryRT, new Rectangle(0, 0, rootOrganismCount, rootOrganismCount), Color.White);
            spriteBatch.End();



            //set back old rendertargets
            graphics.GraphicsDevice.SetRenderTarget(oldRT);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            bool randomMatingFlag = this.randomMatingFlag;
            bool newMigrationFlag = this.newMigrationFlag;
            bool disasterFlag = this.disasterFlag;
            SimulateGeneration(gameTime);  //do particle simulation 
            Vector4 firstOrg = getFirstPixel("organism", organismViewRT, false);
            double genPerSecond = totalMilliseconds != 0 ? generationCount / (totalMilliseconds/1000) : 0;

            string output = String.Format("Enter-> Reset\n" +
                                          "1 -> New Migration:  {0}\n" +
                                          "2 -> RandomMating:   {1}\n" +
                                          "3 -> Disaster:       {2}\n" +
                                          "M -> MutationChance: {3}  {4}\n"+
                                          "N -> KillSelection:   RGBA:{5}\n  {6}\n" +
                                          "\n"+
                                          "TopLeft Organism: \n{7}\n" +
                                          "Generation:       {8}\n"+
                                          "Generations/Sec:  {9}",
                                          newMigrationFlag,
                                          randomMatingFlag,
                                          disasterFlag,
                                          mutationCatcher.currentPercent,
                                          mutationCatcher.active ? "{changing M:" + mutationCatcher.currentInput + "}" : "",
                                          string.Join(", ", natSelectCatcher.numList),
                                          natSelectCatcher.active ? "{changing N:" + natSelectCatcher.currentInput + "}" : "",
                                          string.Format("   R:{0}\n   G:{1}\n   B:{2}\n   A:{3}", firstOrg.X,firstOrg.Y,firstOrg.Z,firstOrg.W ),
                                          generationCount,
                                          genPerSecond);


            graphics.GraphicsDevice.Clear(Color.Black);

            //render with alphablending and make it opaque (if I'm not mistaken)
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
            spriteBatch.Draw(organismViewRT, new Rectangle((int)toCenterRender.X, (int)toCenterRender.Y, rootOrganismCount, rootOrganismCount), Color.White);
            spriteBatch.DrawString(font, output, Vector2.Zero, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }





        private Vector4 getFirstPixel(string label, Texture2D tex, bool print)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, 1, 1);
            Rgba64[] retrievedColor = new Rgba64[1];
            tex.GetData<Rgba64>(
                0,
                sourceRectangle,
                retrievedColor,
                0,
                1);

            if(print)
                Console.WriteLine("{0,14} : {1}", label, retrievedColor[0].ToVector4());
            return retrievedColor[0].ToVector4();
        }
    }
}
