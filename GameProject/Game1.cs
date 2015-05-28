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

		// game objects. Using inheritance would make this
		// easier, but inheritance isn't a GDD 1200 topic
		Burger burger;
		List<TeddyBear> bears = new List<TeddyBear>();
		static List<Projectile> projectiles = new List<Projectile>();
		List<Explosion> explosions = new List<Explosion>();

		// projectile and explosion sprites. Saved so they don't have to
		// be loaded every time projectiles or explosions are created
		static Texture2D frenchFriesSprite;
		static Texture2D teddyBearProjectileSprite;
		static Texture2D explosionSpriteStrip;

		// scoring support
		int score = 0;
		string scoreString = GameConstants.SCORE_PREFIX + 0;

		// health support
		string healthString = GameConstants.HEALTH_PREFIX + 
			GameConstants.BURGER_INITIAL_HEALTH;
		bool burgerDead = false;

		// text display support
		SpriteFont font;

		// sound effects
	
		SoundEffect burgerDamage;
		SoundEffect burgerDeath;
		SoundEffect burgerShot;
		SoundEffect explosion;
		SoundEffect teddyBounce;
		SoundEffect teddyShot;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Assets";

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

			// set resolution
			graphics.PreferredBackBufferWidth = GameConstants.WINDOW_WIDTH;
			graphics.PreferredBackBufferHeight = GameConstants.WINDOW_HEIGHT;
		
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


				burgerDamage = this.Content.Load<SoundEffect> ("sounds/BurgerDamage.wav");
				burgerDeath = this.Content.Load<SoundEffect> ("sounds/BurgerDeath.wav");
				burgerShot = this.Content.Load<SoundEffect> ("sounds/BurgerShot.wav");
				explosion = this.Content.Load<SoundEffect> ("sounds/Explosion.wav");
				teddyBounce = this.Content.Load<SoundEffect> ("sounds/TeddyBounce.wav");
				teddyShot = this.Content.Load<SoundEffect> ("sounds/TeddyShot");




			// load sprite font
			font = Content.Load<SpriteFont> ("Arial20");

			// load projectile and explosion sprites
			frenchFriesSprite = Content.Load<Texture2D>("frenchfries");
			teddyBearProjectileSprite = Content.Load<Texture2D>("teddybearprojectile");
			explosionSpriteStrip = Content.Load<Texture2D>("explosion");

			// add initial game objects
			burger = new Burger(Content, "burger",
			                    graphics.PreferredBackBufferWidth / 2,
			                    graphics.PreferredBackBufferHeight - graphics.PreferredBackBufferHeight / 8,burgerShot);

			for (var i = 0; i < GameConstants.MAX_BEARS; i++)
			{
				SpawnBear();
			}

			// set initial health and score strings
			healthString = GameConstants.HEALTH_PREFIX + burger.Health.ToString ();
			scoreString = GameConstants.SCORE_PREFIX + score.ToString ();
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
		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			KeyboardState keyboard = Keyboard.GetState();

			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
			    keyboard.IsKeyDown(Keys.Escape))
				this.Exit();

			// get current mouse state and update burger

			burger.Update(gameTime, keyboard);

			// update other game objects
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

			// check and resolve collisions between teddy bears
			for (var b1 = 0; b1 < bears.Count; b1++)
			{
				for (var b2 = b1 + 1; b2 < bears.Count; b2++)
				{
					var bear1 = bears[b1];
					var bear2 = bears[b2];

					if (bear1.Active && bear2.Active)
					{
						var collissionInfo = CollisionUtils.CheckCollision(
							gameTime.ElapsedGameTime.Milliseconds,
							GameConstants.WINDOW_WIDTH,
							GameConstants.WINDOW_HEIGHT,
							bear1.Velocity,
							bear1.DrawRectangle,
							bear2.Velocity,
							bear2.DrawRectangle);

						if (collissionInfo != null)
						{
							if (collissionInfo.FirstOutOfBounds)
							{
								bear1.Active = false;
							}
							else
							{
								bear1.Velocity = collissionInfo.FirstVelocity;
								bear1.DrawRectangle = collissionInfo.FirstDrawRectangle;


							}

							if (collissionInfo.SecondOutOfBounds)
							{
								bear2.Active = false;
							}
							else
							{
								bear2.Velocity = collissionInfo.SecondVelocity;
								bear2.DrawRectangle = collissionInfo.SecondDrawRectangle;
							}
						teddyBounce.Play ();

						}
					}
				}
			}

			// check and resolve collisions between burger and teddy bears

			foreach (TeddyBear bear in bears)
			{
				//checking if the bear is active
				if (bear.Active) {
					//checking for the collision 
					if (burger.CollisionRectangle.Intersects (bear.CollisionRectangle)) {
						burgerDamage.Play ();
						burger.Health -= GameConstants.BEAR_DAMAGE;
						CheckBurgerKill ();
						healthString = GameConstants.HEALTH_PREFIX + burger.Health.ToString ();
						bear.Active = false;
						explosions.Add(
							new Explosion(
							explosionSpriteStrip,
							bear.Location.X,bear.Location.Y,explosion));
					}
				}
			}

			// check and resolve collisions between burger and projectiles
			foreach (Projectile projectile in projectiles)
			{
				//checking if the bear is active and projectile is of teddybear
				if (projectile.Active && projectile.Type == ProjectileType.TeddyBear) {
					//checking for the collison
					if (burger.CollisionRectangle.Intersects (projectile.CollisionRectangle)) {
						burgerDamage.Play ();
						burger.Health -= GameConstants.TEDDY_BEAR_PROJECTILE_DAMAGE;
						CheckBurgerKill ();
						healthString = GameConstants.HEALTH_PREFIX + burger.Health.ToString ();
						projectile.Active = false;
					}
				}
			}

			// check and resolve collisions between teddy bears and projectiles
			foreach (var bear in bears)
			{
				if (bear.Active)
				{
					foreach (var projectile in projectiles)
					{
						if (projectile.Active && projectile.Type == ProjectileType.FrenchFries && bear.CollisionRectangle.Intersects(projectile.CollisionRectangle))
						{
							bear.Active = false;
							projectile.Active = false;
							explosions.Add(
								new Explosion(
								explosionSpriteStrip,
								bear.DrawRectangle.Center.X,
								bear.DrawRectangle.Center.Y,explosion));
							score += GameConstants.BEAR_POINTS;
							scoreString = GameConstants.SCORE_PREFIX + score.ToString ();
						}
					}
				}
			}

			// clean out inactive teddy bears and add new ones as necessary
			for (var i = bears.Count - 1; i >= 0; i--)
			{
				if (!bears[i].Active)
				{
					bears.RemoveAt(i);
				}
			}

			// clean out inactive projectiles
			for (var i = projectiles.Count - 1; i >= 0; i--)
			{
				if (!projectiles[i].Active)
				{
					projectiles.RemoveAt(i);
				}
			}

			// clean out finished explosions
			for (var i = explosions.Count - 1; i >= 0; i--)
			{
				if (explosions[i].Finished)
				{
					explosions.RemoveAt(i);
				}
			}

			while (bears.Count < GameConstants.MAX_BEARS) {
				SpawnBear ();
			}

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

			// draw game objects
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

			// draw score and health
			spriteBatch.DrawString(font, healthString, GameConstants.HEALTH_LOCATION, Color.White);
			spriteBatch.DrawString (font, scoreString, GameConstants.SCORE_LOCATION, Color.White);

			spriteBatch.End();

			base.Draw(gameTime);
		}

		#region Public methods

		/// <summary>
		/// Gets the projectile sprite for font, text, position, Color.Whitethe given projectile type
		/// </summary>
		/// <param name="type">the projectile type</param>
		/// <returns>the projectile sprite for the type</returns>
		public static Texture2D GetProjectileSprite(ProjectileType type)
		{
			// replace with code to return correct projectile sprite based on projectile type
			if (type == ProjectileType.FrenchFries)
			{
				return frenchFriesSprite;
			}
			else if (type == ProjectileType.TeddyBear)
			{
				return teddyBearProjectileSprite;
			}
			else
			{
				return frenchFriesSprite;
			}
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
			// generate random location
			int x = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE,
			                          graphics.PreferredBackBufferWidth - 2 * GameConstants.SPAWN_BORDER_SIZE);
			int y = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE,
			                          graphics.PreferredBackBufferHeight - 2 * GameConstants.SPAWN_BORDER_SIZE);

			// generate random velocity
			float speed = GameConstants.MIN_BEAR_SPEED +
				RandomNumberGenerator.NextFloat(GameConstants.BEAR_SPEED_RANGE);
			float angle = RandomNumberGenerator.NextFloat(2 * (float)Math.PI);
			Vector2 velocity = new Vector2(
				(float)(speed * Math.Cos(angle)), (float)(speed * Math.Sin(angle)));

			// create new bear
			TeddyBear newBear = new TeddyBear(Content, "teddybear", x, y, velocity,
			                                  teddyBounce,teddyShot);

			// make sure we don't spawn into a collision
			List<Rectangle> collisionRectangle =  GetCollisionRectangles ();

			while (CollisionUtils.IsCollisionFree(newBear.DrawRectangle,collisionRectangle)) {

				newBear.X = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE,
				                              graphics.PreferredBackBufferWidth - 2 * GameConstants.SPAWN_BORDER_SIZE);

				newBear.Y = GetRandomLocation(GameConstants.SPAWN_BORDER_SIZE,
				                      graphics.PreferredBackBufferHeight - 2 * GameConstants.SPAWN_BORDER_SIZE);
			
			}


			// add new bear to list
			bears.Add(newBear);
		}

		/// <summary>
		/// Gets a random location using the given min and range
		/// 
		/// Example: For a random location between 100 and 700,
		/// pass in 100 for min and 600 for range
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
			if (burger.Health <= 0 && !burgerDead) {
				burgerDead = true;
				burgerDeath.Play ();
			}

		}

		#endregion
	}
}