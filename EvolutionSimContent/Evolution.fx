texture temporaryMap;
sampler temporarySampler : register(s0)  = sampler_state
{
    Texture   = <temporaryMap>;
    MipFilter = None;
    MinFilter = Point;
    MagFilter = Point;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

texture organismMap;
sampler organismSampler  = sampler_state
{
    Texture   = <organismMap>;
    MipFilter = None;
    MinFilter = Point;
    MagFilter = Point;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

texture randomMap;
sampler randomSampler : register(s0) = sampler_state
{
    Texture   = <randomMap>;
    MipFilter = None;
    MinFilter = Point;
    MagFilter = Point;
    AddressU  = wrap;
    AddressV  = wrap;
};

//these just need to exist
struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
};


float organismTexWidth = 0;
float PixOffset = 1;

float elapsedTime = 1.0f;					//keep track of time
float mutationChance = 0/100.0;				//chance of mutation
bool randomMating = true;					//flag to use random mating
float4 selectionColor = float4(0,0,0,0);		//color to be killed off.
int granularity = 10000;



bool IsKilled(float4 organism, float4 select, float4 randVec)
{
    float4 similarity = abs(organism - select) + 0.01;

    //chance to mutate an allele
    float4 randLarge = (randVec*granularity*7+3) % 100;			//random number between 1-100
    float4 selectionChance = (1-similarity)*100;				//range between 0-num that selects if randomNum is within
                                                                //higher difference, smaller range, smaller chance
    int similarCount = 0;
    similarCount += (randLarge.x < selectionChance.x ) ? 1: 0;
    similarCount += (randLarge.y < selectionChance.y ) ? 1: 0;
    similarCount += (randLarge.z < selectionChance.z ) ? 1: 0;
    similarCount += (randLarge.w < selectionChance.w ) ? 1: 0;

    return similarCount >= 3;
}


float4 ResetOrganismsPS(in float2 uv : TEXCOORD0) : COLOR
{
    return tex2D(randomSampler, uv);
}

float4 NewMigrationPS(in float2 uv : TEXCOORD0) : COLOR
{
    //new migration replaces top left organisms
    if(uv.x < 0.1 && uv.y < 0.1 )
    {
        return tex2D(randomSampler, uv);
    }

    return tex2D(organismSampler, uv);
}

float4 DisasterPS(in float2 uv : TEXCOORD0) : COLOR
{
    //all organisms except those in top left died
    float cornerSize = randomMating ? 3*PixOffset : 0.05;
    if(uv.x < cornerSize && uv.y < cornerSize )
    {
        return tex2D(organismSampler, uv);
    }

    return float4(0,0,0,0);
}

float4 UpdateOrganismsPS(in float2 uv : TEXCOORD0) : COLOR
{
    
    float4 pos = tex2D(organismSampler, uv);
    float4 zeroVec = float4(0,0,0,0);

    float4 randVec = tex2D(randomSampler,   uv);		//random value
    float4 randLarge = randVec*granularity;				//create large number from random value
    float4 pairVec = float4(0,0,0,0);					//organism to mate with
    float4 tempPos;

    bool isPosZero  = !any(pos);        //if true, find a primary parent

    if(!isPosZero && any(selectionColor))
    {
        if( IsKilled(pos, selectionColor, randVec) )
        {
            return float4(0,0,0,0);
        }
    }
    
    if(randomMating)  //random mating
    {
        pairVec= tex2D(organismSampler, float2(randVec.y,randVec.w));
        if(isPosZero)
            tempPos = tex2D(organismSampler, float2(randVec.z,randVec.x));
    }
    else			 //nearby mating
    {
        int pairLocation = (int)abs(randLarge.w-randLarge.z) % 9;
        int rowInt = ((pairLocation / 3)-1);
        int colInt = ((pairLocation % 3)-1);
        float row = rowInt*PixOffset + uv.y ;
		float col = colInt*PixOffset + uv.x;;
        pairVec = tex2D(organismSampler, float2(row, col));
        
        if(isPosZero)
        {
            pairLocation = (int)(randLarge.w*2) % 9;
            row = ((pairLocation / 3)-1)*PixOffset + row;
            col = ((pairLocation % 3)-1)*PixOffset + col;
            tempPos = tex2D(organismSampler, float2(row, col));
        }
    }

    if(isPosZero)
    {
        if(!any(tempPos.rgba))	
            return pos;						//no suitable primary parent, return
        pos = tempPos;
    } 
    if(!any(pairVec))
        return pos;							//no suitable pair, return
    

    ///////  1/2 chance to use this organism's or the pair organism's allele.  ////
    int halfGranularity = granularity/2;
    if( ((int)(randVec.w *100))%2 == 0)
    {
        pos.x = (randLarge.x) > halfGranularity ? pairVec.x : pos.x;
        pos.y = (randLarge.y) > halfGranularity ? pairVec.y : pos.y;
        pos.z = (randLarge.z) > halfGranularity ? pairVec.z : pos.z;
        pos.w = (randLarge.w) > halfGranularity ? pairVec.w : pos.w;
    }
    else
    {
        pos.x = (randLarge.x) < halfGranularity ? pairVec.x : pos.x;
        pos.y = (randLarge.y) < halfGranularity ? pairVec.y : pos.y;
        pos.z = (randLarge.z) < halfGranularity ? pairVec.z : pos.z;
        pos.w = (randLarge.w) < halfGranularity ? pairVec.w : pos.w;
    }

    // chance to mutate an allele
    randLarge = (randVec*granularity) % 100;	
    float mutationChanceL = mutationChance*100;
    pos.x = (randLarge.x < mutationChanceL ) ? randVec.x: pos.x;
    pos.y = (randLarge.y < mutationChanceL ) ? randVec.y: pos.y;
    pos.z = (randLarge.z < mutationChanceL ) ? randVec.z: pos.z;
    pos.w = (randLarge.w < mutationChanceL ) ? randVec.w: pos.w;

    return pos;
}

float4 CopyTexturePS(in float2 uv : TEXCOORD0) : COLOR
{
    return tex2D(temporarySampler,uv);
}

//This just needs to exist
float4x4 MatrixTransform : register(vs, c0);
void SpriteVertexShader(inout float4 color    : COLOR0, 
                        inout float2 texCoord : TEXCOORD0, 
                        inout float4 position : SV_Position) 
{ 
    position = mul(position, MatrixTransform); 
} 






technique CopyTexture
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 CopyTexturePS();
    }
}

technique ResetOrganisms
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 ResetOrganismsPS();
    }
}

technique UpdateOrganisms
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 UpdateOrganismsPS();
    }
}

technique NewMigration
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 NewMigrationPS();
    }
}

technique Disaster
{
    pass P0
    {
        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader  = compile ps_3_0 DisasterPS();
    }
}