﻿/*******************************************************************************
* Author    :  Angus Johnson                                                   *
* Date      :  6 April 2022                                                    *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2022                                         *
* License:                                                                     *
* Use, modification & distribution is subject to Boost Software License Ver 1. *
* http://www.boost.org/LICENSE_1_0.txt                                         *
*******************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Clipper2Lib
{

  using PathD = List<PointD>;
  using Paths64 = List<List<Point64>>;
  using PathsD = List<List<PointD>>;

  public class SimpleSvgWriter
  {

    public const uint black = 0xFF000000;
    public const uint white = 0xFFFFFFFF;
    public const uint maroon = 0xFF800000;

    public const uint navy = 0xFF000080;
    public const uint blue = 0xFF0000FF;
    public const uint red = 0xFFFF0000;
    public const uint green = 0xFF008000;
    public const uint yellow = 0xFFFFFF00;
    public const uint lime = 0xFF00FF00;
    public const uint fuscia = 0xFFFF00FF;
    public const uint aqua = 0xFF00FFFF;

    public static RectD RectMax =
      new RectD(double.MaxValue, double.MaxValue, -double.MaxValue, -double.MaxValue);
    public static RectD RectEmpty = new RectD(0, 0, 0, 0);

    internal static bool IsValidRect(RectD rec)
    {
      return rec.right >= rec.left && rec.bottom >= rec.top;
    }

    private readonly struct CoordStyle
    {
      public readonly string FontName;
      public readonly int FontSize;
      public readonly uint FontColor;
      public CoordStyle(string fontname, int fontsize, uint fontcolor)
      {
        FontName = fontname;
        FontSize = fontsize;
        FontColor = fontcolor;
      }
    }

    private readonly struct TextInfo
    {
      public readonly string text;
      public readonly int fontSize;
      public readonly uint fontColor;
      public readonly int posX;
      public readonly int posY;
      public TextInfo(string text, int x, int y,
        int fontsize = 12, uint fontcolor = black)
      {
        this.text = text;
        posX = x;
        posY = y;
        fontSize = fontsize;
        fontColor = fontcolor;
      }
    }

    private readonly struct PolyInfo
    {
      public readonly PathsD paths;
      public readonly uint BrushClr;
      public readonly uint PenClr;
      public readonly double PenWidth;
      public readonly bool ShowCoords;
      public readonly bool IsOpen;
      public PolyInfo(PathsD paths, uint brushcolor, uint pencolor,
        double penwidth, bool showcoords = false, bool isopen = false)
      {
        this.paths = new PathsD(paths);
        BrushClr = brushcolor;
        PenClr = pencolor;
        PenWidth = penwidth;
        ShowCoords = showcoords;
        IsOpen = isopen;
      }
    }

    public FillRule FillRule { get; set; }
    private readonly List<PolyInfo> PolyInfoList = new List<PolyInfo>();
    private readonly List<TextInfo> textInfos = new List<TextInfo>();
    private readonly CoordStyle coordStyle;

    private const string svg_header = "<?xml version=\"1.0\" standalone=\"no\"?>\n" +
      "<svg width=\"{0}px\" height=\"{1}px\" viewBox=\"0 0 {0} {1}\"" +
      " version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\">\n\n";
    private const string svg_path_format = "\"\n style=\"fill:{0};" +
        " fill-opacity:{1:f2}; fill-rule:{2}; stroke:{3};" +
        " stroke-opacity:{4:f2}; stroke-width:{5:f2};\"/>\n\n";
    private const string svg_path_format2 = "\"\n style=\"fill:none; stroke:{0};" +
        "stroke-opacity:{1:f2}; stroke-width:{2:f2};\"/>\n\n";

    public SimpleSvgWriter(FillRule fillrule = FillRule.EvenOdd,
      string coordFontName = "Verdana", int coordFontsize = 9, uint coordFontColor = black)
    {
      coordStyle = new CoordStyle(coordFontName, coordFontsize, coordFontColor);
      FillRule = fillrule;
    }

    public void ClearPaths()
    {
      PolyInfoList.Clear();
    }

    public void ClearText()
    {
      textInfos.Clear();
    }

    public void ClearAll()
    {
      PolyInfoList.Clear();
      textInfos.Clear();
    }


    public void AddPaths(Paths64 paths, bool IsOpen, uint brushColor,
      uint penColor, double penWidth, bool showCoords = false)
    {
      if (paths.Count == 0) return;
      PolyInfoList.Add(new PolyInfo(Clipper.PathsD(paths),
        brushColor, penColor, penWidth, showCoords, IsOpen));
    }
    //------------------------------------------------------------------------------

    public void AddPaths(PathsD paths, bool IsOpen, uint brushColor,
      uint penColor, double penWidth, bool showCoords = false)
    {
      if (paths.Count == 0) return;
      PolyInfoList.Add(new PolyInfo(paths,
        brushColor, penColor, penWidth, showCoords, IsOpen));
    }

    public void AddText(string cap, int posX, int posY, int fontSize, uint fontClr = black)
    {
      textInfos.Add(new TextInfo(cap, posX, posY, fontSize, fontClr));
    }

    private RectD GetBounds()
    {
      RectD bounds = new RectD(RectMax);
      foreach (PolyInfo pi in PolyInfoList)
        foreach (PathD path in pi.paths)
          foreach (PointD pt in path)
          {
            if (pt.x < bounds.left) bounds.left = pt.x;
            if (pt.x > bounds.right) bounds.right = pt.x;
            if (pt.y < bounds.top) bounds.top = pt.y;
            if (pt.y > bounds.bottom) bounds.bottom = pt.y;
          }
      if (!IsValidRect(bounds))
        return RectEmpty;
      else
        return bounds;
    }

    private static string ColorToHtml(uint clr)
    {
      return '#' + (clr & 0xFFFFFF).ToString("X6");
    }

    private static float GetAlpha(uint clr)
    {
      return ((float) (clr >> 24) / 255);
    }

    public bool SaveToFile(string filename, int maxWidth = 0, int maxHeight = 0, int margin = -1)
    {
      if (margin < 0) margin = 20;
      RectD bounds = GetBounds();
      if (bounds.IsEmpty()) return false;

      double scale = 1.0;
      if (maxWidth > 0 && maxHeight > 0)
        scale = Math.Min(
           (maxWidth - margin * 2) / (double)bounds.Width,
            (maxHeight - margin * 2) / (double) bounds.Height);

      long offsetX = margin - (long) (bounds.left * scale);
      long offsetY = margin - (long) (bounds.top * scale);

      StreamWriter writer = new StreamWriter(filename);
      if (writer == null) return false;

      if (maxWidth <= 0 || maxHeight <= 0)
        writer.Write(svg_header, (bounds.right - bounds.left) + margin * 2,
          (bounds.bottom - bounds.top) + margin * 2);
      else
        writer.Write(svg_header, maxWidth, maxHeight);

      foreach (PolyInfo pi in PolyInfoList)
      {
        writer.Write(" <path d=\"");
        foreach (PathD path in pi.paths)
        {
          if (path.Count < 2) continue;
          if (!pi.IsOpen && path.Count < 3) continue;
          writer.Write(string.Format(NumberFormatInfo.InvariantInfo, " M {0:f2} {1:f2}",
              (path[0].x * scale + offsetX),
              (path[0].y * scale + offsetY)));
          for (int j = 1; j < path.Count; j++)
          {
            writer.Write(string.Format(NumberFormatInfo.InvariantInfo, " L {0:f2} {1:f2}",
            (path[j].x * scale + offsetX),
            (path[j].y * scale + offsetY)));
          }
          if (!pi.IsOpen) writer.Write(" z");
        }

        if (!pi.IsOpen)
          writer.Write(string.Format(NumberFormatInfo.InvariantInfo, svg_path_format,
              ColorToHtml(pi.BrushClr), GetAlpha(pi.BrushClr),
              (FillRule == FillRule.EvenOdd ? "evenodd" : "nonzero"),
              ColorToHtml(pi.PenClr), GetAlpha(pi.PenClr), pi.PenWidth));
        else
          writer.Write(string.Format(NumberFormatInfo.InvariantInfo, svg_path_format2,
              ColorToHtml(pi.PenClr), GetAlpha(pi.PenClr), pi.PenWidth));

        if (pi.ShowCoords)
        {
          writer.Write(string.Format("<g font-family=\"{0}\" font-size=\"{1}\" fill=\"{2}\">\n",
            coordStyle.FontName, coordStyle.FontSize, ColorToHtml(coordStyle.FontColor)));
          foreach (PathD path in pi.paths)
          {
            foreach (PointD pt in path)
            {
#if USINGZ
              writer.Write(string.Format(
                  "<text x=\"{0}\" y=\"{1}\">{2},{3},{4}</text>\n",
                  (int)(pt.x * scale + offsetX), (int)(pt.y * scale + offsetY), pt.x, pt.y, pt.z));
#else
              writer.Write(string.Format(
                  "<text x=\"{0:f2}\" y=\"{1:f2}\">{2},{3}</text>\n",
                  (pt.x * scale + offsetX), (pt.y * scale + offsetY), pt.x, pt.y));
#endif
            }
          }
          writer.Write("</g>\n\n");
        }
      }

      foreach (TextInfo captionInfo in textInfos)
      {
        writer.Write(string.Format(
            "<g font-family=\"Verdana\" font-style=\"normal\" " +
            "font-weight=\"normal\" font-size=\"{0}\" fill=\"{1}\">\n",
            captionInfo.fontSize, ColorToHtml(captionInfo.fontColor)));
        writer.Write(string.Format(
            "<text x=\"{0}\" y=\"{1}\">{2}</text>\n</g>\n",
            (int) (captionInfo.posX + margin),
            (int) (captionInfo.posY + margin),
            captionInfo.text));
      }

      writer.Write("</svg>\n");
      writer.Close();
      return true;
    }
  }

}
