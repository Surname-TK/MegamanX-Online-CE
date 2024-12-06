using System.Collections.Generic;

namespace MMXOnline;

public class LightningWeb : Weapon {

	public static LightningWeb netWeapon = new();
	public LightningWeb()
	{
		shootSounds = new string[] { "busterX4", "busterX4", "busterX4", "busterX4" };
		fireRate = 60;
		index = (int)WeaponIds.LightningWeb;
		weaponBarBaseIndex = 72;
		weaponBarIndex = 61;
		weaponSlotIndex = 123;
		//killFeedIndex = 181;
		switchCooldownFrames = 9;
		weaknessIndex = (int)WeaponIds.TwinSlasher;
		/* damage = "1/1";
		hitcooldown = "0.75";
		Flinch = "6/26";
		FlinchCD = hitcooldown;
		effect = "Can be used as a wall. C: Creates a network of nine webs."; */
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new LightningWebProj(this, pos, xDir, player, player.getNextActorNetId(), true);
		} else {
			new LightningWebProjCharged(this, pos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}

public class LightningWebProj : Projectile {
	public Character character;

	public LightningWebProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 250, 1, player, "lightningweb_proj", 
		Global.miniFlinch, 0.75f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.25f;
		projId = (int)ProjIds.LightningWebProj;
		destroyOnHit = false;
		character = player.character;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProj(
			LightningWeb.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		
		if (character.player.input.isPressed(Control.Shoot, character.player)) {
			destroySelf();
		}
	}
	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer) {
			new LightningWebProjWeb(weapon, pos, xDir, base.owner, base.owner.getNextActorNetId(), rpc: true);
		}
	}
}


public class LightningWebProjWeb : Projectile {

	Wall wall;
	public LightningWebProjWeb(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 0, player, "lightningweb_proj_web", 
		Global.halfFlinch, 1f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1f;
		projId = (int)ProjIds.LightningWeb;
		setIndestructableProperties();
		fadeSprite = "lightningweb_webexausth";
		fadeOnAutoDestroy = true;
		playSound("lightningWebProjWeb", sendRpc: true);
		collider.isClimbable = true;
		collider.wallOnly = false;
		isStatic = true;
		
		var rect = collider.shape.getRect().getPoints();
		wall = new Wall("Collision Shape", new List<Point>()
		{
				rect[0].add(new Point(0, 0)),
				rect[1].add(new Point(0, 0)),
				rect[2].add(new Point(0, 0)),
				rect[3].add(new Point(0, 0)),
			});

		Global.level.addGameObject(wall);
		
		if (player.character != null) zIndex = player.character.zIndex - 10;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProjWeb(
			LightningWeb.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (wall != null) Global.level.removeGameObject(wall);
	}
}


public class LightningWebProjCharged : Projectile {
	public Character character;

	public LightningWebProjCharged(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 250, 0, player, "lightningweb_proj", 
		0, 0f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.5f;
		projId = (int)ProjIds.LightningWebChargedProj;
		//destroyOnHit = false;
		character = player.character;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProjCharged(
			LightningWeb.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		 
		if (character.player.input.isPressed(Control.Shoot, character.player)) {
			destroySelf();
		}
	}
	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer) {
			new LightningWebProjWebCharged(
				weapon, pos, xDir, damager.owner, 0, damager.owner.getNextActorNetId(), true);
		}
	}
}

public class LightningWebProjWebCharged : Projectile {
	private float lastHitTime;
	private int type;
	bool fired;
	float moveTime;
	Player player;

	public LightningWebProjWebCharged(
		Weapon weapon, Point pos, int xDir, Player player, 
		int type, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0f, 1f, player, "lightningweb_proj_charged", 
		Global.miniFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = type == 0 ? 1.5f : 1;
		projId = (int)ProjIds.LightningWebCharged;
		fadeSprite = "lightningweb_proj_chargedexausth";
		fadeOnAutoDestroy = true;
		if (type == 0) playSound("lightningWebProjWeb", sendRpc: true);
		destroyOnHit = false;
		this.type = type;
		this.player = player;
		
		if (player.character != null) zIndex = player.character.zIndex + 10;
		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningWebProjWebCharged(
			LightningWeb.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (type == 0) {
			if (time >= maxTime / 3 && !fired) {
				fired = true;
				playSound("lightningWebProjWeb", sendRpc: true);
				for (int i = 0; i < 8; i++) {
					if (ownedByLocalPlayer) {
						new LightningWebProjWebCharged(weapon, pos, xDir, 
						damager.owner, i + 1, damager.owner.getNextActorNetId(), true)
						{frameIndex = frameIndex, frameTime = frameTime};
					}
				}
				if (player.hasPlasma()) {
					new BusterForcePlasmaHit(
						6, weapon, pos, xDir, damager.owner,
						damager.owner.getNextActorNetId(), true
					);
				}
			}
		} else {
			if (moveTime < 16  || time >= maxTime - (Global.spf * 20)) {
				move(Point.createFromByteAngle((type - 1) * 32) * 240);
			}
			moveTime++;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		lastHitTime = 0.2f;
		if (damagable is Character chr && chr.ownedByLocalPlayer && !chr.isImmuneToKnockback()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 25f);
			chr.slowdownTime = 0.25f;
		}
		base.onHitDamagable(damagable);
	}
}
