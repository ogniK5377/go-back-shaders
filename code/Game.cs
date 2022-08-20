using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoBack;

public class GoBackPostProcess : BasePostProcess
{
	private int lastWidth = 320;
	private int lastHeight = 240;

	private int currentWidth = 320;
	private int currentHeight = 240;

	public int DesiredWidth
	{
		get => currentWidth;
		set
		{
			currentWidth = value;
			if ( currentWidth != lastWidth ) BuildTextureTargets();
		}
	}

	public int DesiredHeight
	{
		get => currentHeight;
		set
		{
			currentHeight = value;
			if ( currentHeight != lastHeight ) BuildTextureTargets();
		}
	}

	private Texture DownscaledScreen;
	private Material GoBackPP;

	public override PostProcessPass Passes => PostProcessPass.Sdr;

	public GoBackPostProcess() : base()
	{
		Host.AssertClient();

		GoBackPP = Material.Load( "materials/post_process/goback_postprocess.vmat" );

		BuildTextureTargets();
	}

	private void BuildTextureTargets()
	{
		DownscaledScreen = Texture.CreateRenderTarget( "GoBack_Downscale", ImageFormat.RGBA8888, new Vector2( DesiredWidth, DesiredHeight ) );
		lastWidth = currentWidth;
		lastHeight = currentHeight;
	}

	public override void Render()
	{
		Sandbox.Render.Material = GoBackPP;

		Attributes.Set( "InternalResolution", new Vector2( currentWidth, currentHeight ) );
		Attributes.SetCombo( "D_PASS", 0 );
		// Downscale to internal resolution
		using ( ScopedRenderTarget() )
		{
			Sandbox.Render.SetViewport( 0, 0, DesiredWidth, DesiredHeight );
			Sandbox.Render.SetRenderTarget( DownscaledScreen );
			RenderScreenQuad( false );
		}

		// Upscale to native resolution & dither
		Attributes.SetCombo( "D_PASS", 1 );
		Attributes.Set( "DownscaleBuffer", DownscaledScreen );
		RenderScreenQuad( true );
	}
}

public partial class GoBackGame : Sandbox.Game
{
	private GoBackPostProcess PostProcessMaterial { get; set; }
	public GoBackGame()
	{
		if ( IsClient )
		{
			PostProcessMaterial = new();
			PostProcessMaterial.DesiredWidth = 640;
			PostProcessMaterial.DesiredHeight = 480;
			PostProcess.Add( PostProcessMaterial );
		}
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Pawn();
		client.Pawn = pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}
}
