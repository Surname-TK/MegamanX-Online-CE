using System.Collections.Generic;

namespace MMXOnline;

public enum CapsuleType {
    Upgrade,
    Hyper,
    RideArmor
}

public class LightCapsule : Actor {
    public ShaderWrapper lightCapsuleShader = Helpers.cloneGenericPaletteShader("paletteLightCapsule");
    //Wall wall;
    public CapsuleType capsuleType;
    
	public LightCapsule(Player owner, Point pos, string sprite, ushort? netId, bool ownedByLocalPlayer, NetActorCreateId netActorCreateId, bool sendRpc = false) :
		base(sprite, pos, netId, ownedByLocalPlayer, false) {
		netOwner = owner;
		collider.wallOnly = false;
        collider.isClimbable = true;
		collider.isTrigger = false;
		zIndex = ZIndex.MainPlayer + 10;
		
		/*var rect = collider.shape.getRect().getPoints();
		wall = new Wall("Collision Shape", new List<Point>()
		{
				rect[0].add(new Point(0, 0)),
				rect[1].add(new Point(0, 0)),
				rect[2].add(new Point(0, 0)),
				rect[3].add(new Point(0, 0)),
			});

		Global.level.addGameObject(wall);*/

		this.netActorCreateId = netActorCreateId;
		if (sendRpc) {
			createActorRpc(owner.id);
		}
	}

	public override void update() {
		base.update();
		var leeway = 500;
		if (ownedByLocalPlayer && pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway) {
			destroySelf();
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = new();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		if (capsuleType == CapsuleType.Hyper) {
			palette = lightCapsuleShader;
			palette?.SetUniform("palette", 4);
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}

	/*public override void onDestroy() {
		base.onDestroy();
		if (wall != null) Global.level.removeGameObject(wall);
	}*/
}

public class UpgradeCapsule : LightCapsule {
	public UpgradeCapsule(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "light_capsule", netId, ownedByLocalPlayer,
		NetActorCreateId.UpgradeCapsule, sendRpc: sendRpc
	) {
		capsuleType = CapsuleType.Upgrade;
	}
}

public class HyperCapsule : LightCapsule {
	public HyperCapsule(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "light_capsule", netId, ownedByLocalPlayer,
		NetActorCreateId.HyperCapsule, sendRpc: sendRpc
	) {
		capsuleType = CapsuleType.Hyper;
	}
}