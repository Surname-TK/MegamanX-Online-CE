namespace MMXOnline;

public class SubTank {
	public float health;
	public float healthOnUse = 14;
	public const float maxHealth = 14;
	public bool isInUse;
	public Player player;
	public SubTank() {
	}

	public void use(Character character) {
		healthOnUse = health;
		character.addHealthSubtank(health);
		character.usedSubtank = this;
		RPC.useSubtank.sendRpc(character.netId, (int)health);
	}

	public void use(Maverick maverick) {
		maverick.addHealthSubtank(health);
		maverick.usedSubtank = this;
		RPC.useSubtank.sendRpc(maverick.netId, (int)health);
	}
}
