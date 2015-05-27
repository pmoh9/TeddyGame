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

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Game objects
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // Projectile and explosion sprites
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // Scoring support
        int score = 0;
        string scoreString = GameConstants.SCORE_PREFIX + 0;

        // Health support
        string healthString = GameConstants.HEALTH_PREFIX + 
            GameConstants.BURGER_INITIAL_HEALTH;
        bool burgerDead = false;
        string gameOverString = "Game Over!";

        // Pause support
        bool isPaused = false;
        bool keyReleased = true;
        bool keyPressStarted = false;
        string pauseString = "Game Paused";
        
        // New Game support
        bool RkeyReleased = true;
        bool RkeyPressStarted = false;
        string helpString = "Use Arrow keys to move the burger and Space to shoot";
        string helpString2 = "Press R to restart or Q to quit";
        
        // Text display support
        SpriteFont font;

        // Sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosion;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WINDOW_WIDTH;
            graphics.PreferredBackBufferHeight = GameConstants.WINDOW_HEIGHT;
            graphics.IsFullScreen = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load audio content
            burgerDamage = Content.Load<SoundEffect>("sounds\\BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>("sounds\\BurgerDeath");
            burgerShot = Content.Load<SoundEffect>("sounds\\BurgerShot");
            explosion = Content.Load<SoundEffect>("sounds\\Explosion");
            teddyBounce = Content.Load<SoundEffect>("sounds\\TeddyBounce");
            teddyShot = Content.Load<SoundEffect>("sounds\\TeddyShot");

            // Load sprite font
            font = Content.Load<SpriteFont>("Arial20");

            // Load projectile and explosion sprites
            teddyBearProjectileSprite = Content.Load<Texture2D>("teddybearprojectile");
            frenchFriesSprite = Content.Load<Texture2D>("frenchfries");
            explosionSpriteStrip = Content.Load<Texture2D>("explosion");

            // Add initial game objects
            burger = new Burger(Content, "burger", GameConstants.WINDOW_WIDTH / 2, GameConstants.WINDOW_HEIGHT * 7 / 8, burgerShot);
            for (int i = 0; i < GameConstants.MAX_BEARS; i++)
                SpawnBear();

            // Set initial health and score strings
            healthString = GameConstants.HEALTH_PREFIX + burger.Health;
            scoreString = GameConstants.SCORE_PREFIX + score;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
                projectiles.RemoveAt(i);
            for (int i = GameConstants.MAX_BEARS - 1; i >= 0; i--)
                    bears.RemoveAt(i);
            for (int i = explosions.Count - 1; i >= 0; i--)
                explosions.RemoveAt(i);

            score = 0;
            healthString = "";
            scoreString = "";
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState key = Keyboard.GetState();
            
            // Allows the game to exit
            if (key.IsKeyDown(Keys.Q))
                this.Exit();

            // Restart Game
            if (key.IsKeyDown(Keys.R) && RkeyReleased)
            {
                RkeyPressStarted = true;
                RkeyReleased = false;
            }
            else if (key.IsKeyUp(Keys.R))
            {
                RkeyReleased = true;
                if (RkeyPressStarted)
                {
                    RkeyPressStarted = false;
                    UnloadContent();
                    burgerDead = false;
                    LoadContent();
                }
            }

            // Pause Game
            if (key.IsKeyDown(Keys.Escape) && keyReleased)
            {
                keyPressStarted = true;
                keyReleased = false;
            }
            else if (key.IsKeyUp(Keys.Escape))
            {
                keyReleased = true;
                if (keyPressStarted && !burgerDead)
                {
                    keyPressStarted = false;
                    isPaused = !isPaused;
                }
            }
            if (isPaused)
                return;
            else if (!burgerDead)
                helpString = "Use Arrow keys to move the burger and Space to shoot";
            
            // Update burger
            burger.Update(gameTime, key);
            
            // Update other game objects
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // Check and resolve collisions between teddy bears
            CollisionResolutionInfo collisionInfo = null;
            for (int i = 0; i < bears.Count; i++)
            {
                for (int j = i + 1; j < bears.Count; j++)
                {
                    if (bears[i].Active && bears[j].Active)
                    {
                        collisionInfo = CollisionUtils.CheckCollision(gameTime.ElapsedGameTime.Milliseconds,
                            GameConstants.WINDOW_WIDTH, GameConstants.WINDOW_HEIGHT, bears[i].Velocity, bears[i].DrawRectangle,
                            bears[j].Velocity, bears[j].DrawRectangle);
                    }
                    if (collisionInfo != null)
                    {
                        teddyBounce.Play(0f,0f,0f);
                        if (collisionInfo.FirstOutOfBounds)
                            bears[i].Active = false;
                        else
                        {
                            bears[i].Velocity = collisionInfo.FirstVelocity;
                            bears[i].DrawRectangle = collisionInfo.FirstDrawRectangle;
                        }
                        if (collisionInfo.SecondOutOfBounds)
                            bears[j].Active = false;
                        else
                        {
                            bears[j].Velocity = collisionInfo.SecondVelocity;
                            bears[j].DrawRectangle = collisionInfo.SecondDrawRectangle;
                        }
                    }
                }
            }

            // Check and resolve collisions between burger and teddy bears
            foreach (TeddyBear bear in bears)
                if (!burgerDead && bear.Active && bear.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    burger.Health -= GameConstants.BEAR_DAMAGE;
                    burgerDamage.Play(0.1f,0f,0f);
                    healthString = GameConstants.HEALTH_PREFIX + burger.Health;
                    CheckBurgerKill();
                    bear.Active = false;
                    explosions.Add(new Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y, explosion));
                }

            // Check and resolve collisions between burger and projectiles
            foreach (Projectile projectile in projectiles)
                if (!burgerDead && projectile.Type == ProjectileType.TeddyBear && projectile.Active &&
                    projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    burger.Health -= GameConstants.TEDDY_BEAR_PROJECTILE_DAMAGE;
                    burgerDamage.Play(0.1f, 0f, 0f);
                    healthString = GameConstants.HEALTH_PREFIX + burger.Health;
                    CheckBurgerKill();
                    projectile.Active = false;
                }

            // Check and resolve collisions between teddy bears and projectiles
            foreach (TeddyBear bear in bears)
                foreach (Projectile projectile in projectiles)
                    if (projectile.Type == ProjectileType.FrenchFries &&
                        bear.CollisionRectangle.Intersects(projectile.CollisionRectangle) &&
                        bear.Active && projectile.Active)
                    {
                        bear.Active = false;
                        projectile.Active = false;
                        explosions.Add(new Explosion(explosionSpriteStrip, bear.Location.X, bear.Location.Y, explosion));
                        score += GameConstants.BEAR_POINTS;
                        scoreString = GameConstants.SCORE_PREFIX + score;
                    }

            // Clean out inactive teddy bears and add new ones as necessary
            for (int i = 0; i < bears.Count; i++)
                if (!bears[i].Active)
                    bears.RemoveAt(i--);

            while (bears.Count < GameConstants.MAX_BEARS)
                SpawnBear();

            // Clean out inactive projectiles
            for (int i = 0; i < projectiles.Count; i++)
                if (!projectiles[i].Active)
                    projectiles.RemoveAt(i--);

            // Clean out finished explosions
            for (int i = 0; i < explosions.Count; i++)
                if (explosions[i].Finished)
                    explosions.RemoveAt(i--);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            spriteBatch.DrawString(font, helpString, GameConstants.HELP_LOCATION, Color.White);
            
            // Draw game objects
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }

            // Draw score and health
            spriteBatch.DrawString(font, healthString, GameConstants.HEALTH_LOCATION, Color.White);
            spriteBatch.DrawString(font, scoreString, GameConstants.SCORE_LOCATION, Color.White);

            if (burgerDead)
            {
                helpString = "";
                spriteBatch.DrawString(font, gameOverString, GameConstants.GAMESTRING_LOCATION, Color.White);
                spriteBatch.DrawString(font, helpString2, GameConstants.HELP2_LOCATION, Color.White);
            }

            if (isPaused)
            {
                helpString = "";
                spriteBatch.DrawString(font, pauseString, GameConstants.GAMESTRING_LOCATION, Color.White);
            }
            
            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // Replace with code to return correct projectile sprite based on projectile type
            if (type == ProjectileType.TeddyBear)
                return teddyBearProjectileSprite;
            else
                return frenchFriesSprite;
        }

        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // Generate random location
            int x = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_WIDTH - 2 * GameConstants.SPAWN_BORDER_SIZE);
            int y = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_HEIGHT - 2 * GameConstants.SPAWN_BORDER_SIZE);

            // Generate random velocity
            float speed = RandomNumberGenerator.NextFloat(GameConstants.BEAR_SPEED_RANGE);
            if (speed < GameConstants.MIN_BEAR_SPEED)
                speed = GameConstants.MIN_BEAR_SPEED;
            float angle = RandomNumberGenerator.NextFloat(2 * (float)Math.PI);
            Vector2 velocity = new Vector2(speed * (float)Math.Cos(angle), speed * (float)Math.Sin(angle));

            // Create new bear
            TeddyBear newBear = new TeddyBear(Content, "teddybear", x, y, velocity, teddyBounce, teddyShot);

            // Make sure we don't spawn into a collision
            List<Rectangle> collisionRectangles = GetCollisionRectangles();
            while (!CollisionUtils.IsCollisionFree(newBear.CollisionRectangle, collisionRectangles))
            {
                newBear.X = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_WIDTH - 2 * GameConstants.SPAWN_BORDER_SIZE);
                newBear.Y = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE, GameConstants.WINDOW_HEIGHT - 2 * GameConstants.SPAWN_BORDER_SIZE);
            }

            // Add new bear to list
            bears.Add(newBear);

        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.Health == 0 && !burgerDead)
            {
                burgerDead = true;
                burgerDeath.Play();
            }
        }

        #endregion
    }
}
