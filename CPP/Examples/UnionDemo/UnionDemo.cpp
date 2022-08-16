
#include <cstdlib>
#include "../../Clipper2Lib/clipper.h"
#include "../../Utils/clipper.svg.h"
#include "../../Utils/clipper.svg.utils.h"

using namespace std;
using namespace Clipper2Lib;


void System(const std::string &filename);

int main(int argc, char* argv[]) {
  Paths64 subject, clip, solution;
  subject.push_back(MakePath("100, 50, 10, 79, 65, 2, 65, 98, 10, 21"));
  clip.push_back(MakePath("98, 63, 4, 68, 77, 8, 52, 100, 19, 12"));
  solution = Union(subject, clip, FillRule::NonZero);  
  
  //draw solution
  SvgWriter svg;
  FillRule fr = FillRule::NonZero;
  SvgAddSolution(svg, solution, fr, false);
  SvgSaveToFile(svg, "union.svg", 450, 450, 10);
  System("union.svg");

  return 0;
}

//---------------------------------------------------------------------------

void System(const std::string &filename)
{
#ifdef _WIN32
  system(filename.c_str());
#else
  system(("google-chrome " + filename).c_str());
#endif
}

